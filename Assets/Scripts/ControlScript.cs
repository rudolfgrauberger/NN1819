using System;
using System.Collections;
using System.Collections.Generic;
using NeuralNetwork;
using UnityEngine;

public class ControlScript : MonoBehaviour {

    private Rigidbody rigid;

    public float GroundDistance, StabilizingForce, SidewaysForce, JumpForce, MinimumClearance;
    private float steeringRightForce, steeringLeftForce, jumpingForce;
        
    private bool isJumping, justCrashed;
    
    public ControlMode controlMode;

    Vector3 initialPosition;
    Quaternion initialRotation;

    private static NeuralNet network;
    private SensorSuite sensors;

    private readonly static String  RECORD_FILE = "./RecordedData/Default.csv";
    private static Dictionary<Command, List<DataSet>> recordedData;

    private readonly static double[] LEFT_VECTOR = { 1.0, 0.0, 0.0 };
    private readonly static double[] RIGHT_VECTOR = { 0.0, 1.0, 0.0 };
    private readonly static double[] JUMP_VECTOR = { 0.0, 0.0, 1.0 };
    private readonly static double[] NULL_VECTOR = { 0.0, 0.0, 0.0 };


    // Use this for initialization
    void Start () {
        rigid = GetComponent<Rigidbody>();
        initialPosition = transform.position;
        initialRotation = transform.rotation;

        // 6 sensor inputs, 3 (commands) output
        network = new NeuralNet(6, 8, 3);
        sensors = GameObject.FindObjectOfType<SensorSuite>();
        recordedData = new Dictionary<Command, List<DataSet>>();
        recordedData.Add(Command.Left, new List<DataSet>());
        recordedData.Add(Command.Right, new List<DataSet>());
        recordedData.Add(Command.Jump, new List<DataSet>());
        recordedData.Add(Command.Empty, new List<DataSet>());

        LoadDataFromTrainSet();

        if (controlMode == ControlMode.automatic)
        {
            TrainNetwork();
        }
    }

    private Command GetCommandFromOutput(double[] values)
    {
        int index = -1;
        double biggestValue = 0.0;

        for(int i = 0; i < values.Length; ++i)
        {
            if (biggestValue < values[i])
            {
                index = i;
                biggestValue = values[i];
            }
        }

        if (biggestValue > 0.5)
            return (Command)index;
        else
            return Command.Empty;
    }
    
    // Update is called once per frame
    void Update () {
        if (controlMode ==  ControlMode.automatic)
        {
            ControlThroughTheNeuralNetwork();
            return;
        }

        if (controlMode == ControlMode.manual)
        {
            if (Input.GetKey(KeyCode.W))
                Time.timeScale = 1.0f;
            if (Input.GetKey(KeyCode.S))
                Time.timeScale = 0.05f;
            if (Input.GetKey(KeyCode.X))
            {
                SaveRecordedData();
                if (recordedData.Count == 0)
                {
                    Application.Quit();
                    return;
                }
            }
        }

        double[] currentVector = NULL_VECTOR;

        if (Input.GetKey(KeyCode.A)) {
            SteerLeft(1, true);
            currentVector = LEFT_VECTOR;
        }
        if (Input.GetKey(KeyCode.D)) {
            SteerRight(1, true);
            currentVector = RIGHT_VECTOR;
        }
        if (Input.GetKeyDown(KeyCode.Space)) {
            Jump(1, true);
            currentVector = JUMP_VECTOR;
        }

        recordedData[GetCommand(currentVector)].Add(new DataSet(GetCurrentSensorVector(), currentVector));
    }

    private void ControlThroughTheNeuralNetwork()
    {
        double[] input = GetCurrentSensorVector();
        double[] output = network.Compute(input);

        Debug.Log(string.Format("Left: {0:F2}\tRight: {1:F2}\tJump: {2:F2}", output[0], output[1], output[2]));

        switch (GetCommandFromOutput(output))
        {
            case Command.Left:
                {
                    if (output[(int)Command.Left] > 0.1)
                        SteerLeft(1, false);
                }
                break;
            case Command.Right:
                {
                    if (output[(int)Command.Right] > 0.1)
                        SteerRight(1, false);
                }
                break;
            case Command.Jump:
                {
                    if (output[(int)Command.Jump] > 0.01)
                    {
                        Jump(1, false);
                    }
                }
                break;
            default:
                break;
        }
    }

    private void FixedUpdate()
    {
        RaycastHit hit;
        if(Physics.Raycast(transform.position, Vector3.down, out hit))
        {
            float groundDistance = Vector3.Distance(transform.position, hit.point);
            float repulsionFactor = GroundDistance- groundDistance;
            if (repulsionFactor > 0)
                isJumping = false;
            rigid.AddRelativeForce(Vector3.up * StabilizingForce*repulsionFactor);
        }
            rigid.AddRelativeForce(Vector3.right * SidewaysForce*steeringRightForce);
            rigid.AddRelativeForce(Vector3.left * SidewaysForce*steeringLeftForce);
        if (!isJumping && jumpingForce-.1f>0f)
        {
            isJumping = true;
            Vector3 nVelocity = rigid.velocity;
            nVelocity.y = JumpForce*jumpingForce;
            rigid.velocity = nVelocity;
        }
        StopSteering();
    }

    public void OnCollisionEnter(Collision collision)
    {
        if (!justCrashed)
            StartCoroutine(Reset());
    }

    public void SteerRight(float force, bool manualOverride)
    {
        if (controlMode == ControlMode.automatic && manualOverride)
            return;
        steeringRightForce = Mathf.Clamp01(force);

        /*if (controlMode == ControlMode.manual && IsBalancedAfterInsert(Command.Right))
            recordedData[Command.Right].Add(new DataSet(GetCurrentSensorVector(), RIGHT_VECTOR));*/
    }

    public void SteerLeft(float force, bool manualOverride)
    {
        if (controlMode == ControlMode.automatic && manualOverride)
            return;

        steeringLeftForce = Mathf.Clamp01(force);

        /*if (controlMode == ControlMode.manual && IsBalancedAfterInsert(Command.Left))
            recordedData[Command.Left].Add(new DataSet(GetCurrentSensorVector(), LEFT_VECTOR));*/
    }

    public void Jump(float force, bool manualOverride)
    {
        if (controlMode == ControlMode.automatic && manualOverride)
            return;
        jumpingForce = Mathf.Clamp01(force);

        /*if (controlMode == ControlMode.manual && IsBalancedAfterInsert(Command.Jump))
            recordedData[Command.Jump].Add(new DataSet(GetCurrentSensorVector(), JUMP_VECTOR));*/
    }
    public void StopSteering()
    {
        steeringRightForce=0;
        steeringLeftForce=0;
        jumpingForce=0;
    }

    private IEnumerator Reset()
    {
        justCrashed = true;
        yield return new WaitForSeconds(1);
        transform.position = initialPosition;
        transform.rotation = initialRotation;
        rigid.angularVelocity = Vector3.zero;
        rigid.velocity = Vector3.zero;
        justCrashed = false;
    }

    private void LoadDataFromTrainSet()
    {
        if (!System.IO.File.Exists(RECORD_FILE))
            return;

        int lineCount = 0;
        System.IO.StreamReader file = new System.IO.StreamReader(RECORD_FILE);
        String line = String.Empty;

        while ((line = file.ReadLine()) != null)
        {
            ++lineCount;
            String[] parts_of_line = line.Split(',');

            if (parts_of_line.Length != 9)
            {
                throw new FormatException(
                    string.Format(
                        "Ungültige Spaltenanzahl in Zeile {0} der Datei {1}. (Erwartet: 9, Tatsächlich: {2}", 
                        lineCount,
                        RECORD_FILE,
                        parts_of_line.Length
                    )
                );
            }

            double[] sensorValues = new double[6];
            double[] commandValues = new double[3];

            for (int i = 0; i < parts_of_line.Length; ++i)
            {
                double value = Convert.ToDouble(parts_of_line[i].Trim());
                WriteDataIntoArray(i, value, sensorValues, commandValues);
            }

            recordedData[GetCommand(commandValues)].Add(new DataSet(sensorValues, commandValues));
        }
    }

    private void WriteDataIntoArray(int index, double value, double[] sensorArray, double[] commandArray)
    {
        if (index < 6)
            sensorArray[index] = value;
        else
            commandArray[index % 3] = value;
    }

    private void SaveRecordedData()
    {
        List<DataSet> datasetCollector = new List<DataSet>();
        datasetCollector.AddRange(recordedData[Command.Left]);
        datasetCollector.AddRange(recordedData[Command.Right]);
        datasetCollector.AddRange(recordedData[Command.Jump]);
        datasetCollector.AddRange(recordedData[Command.Empty]);

        recordedData.Clear();

        if (datasetCollector.Count == 0)
            return;

        string path = "./TrainingData";

        if (!System.IO.Directory.Exists(path))
            System.IO.Directory.CreateDirectory(path);

        string filepath = string.Format("{0}/record-{1}.csv", path, DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss"));
        System.IO.StreamWriter file = new System.IO.StreamWriter(filepath);
        String line = String.Empty;

        foreach (var item in datasetCollector)
        {
            string values = GetCommaSeperatedStringFromVector(item.Values);
            string targets = GetCommaSeperatedStringFromVector(item.Targets);
            file.WriteLine("{0},{1}", values, targets);
            file.Flush();
        }

        file.Close();
    }

    private bool IsBalancedAfterInsert(Command command)
    {
        return true;
        int leftCount = 0;
        int rightCount = 0;
        int jumpCount = 0;

        if (recordedData.ContainsKey(Command.Left))
            leftCount = recordedData[Command.Left].Count;

        if (recordedData.ContainsKey(Command.Right))
            rightCount = recordedData[Command.Right].Count;

        if (recordedData.ContainsKey(Command.Jump))
            jumpCount = recordedData[Command.Jump].Count;

        Debug.Log(string.Format("Left: {0}\tRight: {1}\tJump: {2}", leftCount, rightCount, jumpCount));

        int sumCount = leftCount + rightCount + jumpCount;

        // Alle haben gleich viele Elemente
        if (sumCount % 3 == 0)
            return true;

        int maxValue = Math.Max(leftCount, rightCount);
        maxValue = Math.Max(maxValue, jumpCount);

        // Muss kleiner als der größte Wert sein
        if (recordedData[command].Count < maxValue)
            return true;
        else
            return false;
    }

    private Command GetCommand(double[] target)
    {
        if (target[(int)Command.Left] == 1) return Command.Left;
        else if (target[(int)Command.Right] == 1) return Command.Right;
        else if (target[(int)Command.Jump] == 1) return Command.Jump;
        else return Command.Empty;
    }

    private string GetCommaSeperatedStringFromVector(double[] values)
    {
        return string.Join(",", Array.ConvertAll<double, string>(values, Convert.ToString));
    }

    private double[] GetCurrentSensorVector()
    {
        return new double[] {
            sensors.DistLeft,
            sensors.DistRight,
            sensors.DistLeftCentral,
            sensors.DistRightCentral,
            sensors.DistGround,
            sensors.FlierLateralPosition
        };
    }

    private void TrainNetwork()
    {
        List<DataSet> datasetCollector = new List<DataSet>();
        datasetCollector.AddRange(recordedData[Command.Left]);
        datasetCollector.AddRange(recordedData[Command.Right]);
        datasetCollector.AddRange(recordedData[Command.Jump]);
        datasetCollector.AddRange(recordedData[Command.Empty]);

        network.Train(datasetCollector, 0.08);
    }
}

public enum Command
{
    Left,
    Right,
    Jump,
    Empty
}

public enum ControlMode
{
    manual,
    automatic
};
