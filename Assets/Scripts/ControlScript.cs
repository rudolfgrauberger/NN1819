using System;
using System.Collections;
using System.Collections.Generic;
using NeuralNetwork;
using UnityEngine;

public class ControlScript : MonoBehaviour {

    private Rigidbody rigid;

    public float GroundDistance, StabilizingForce, SidewaysForce, JumpForce, MinimumClearance;
    public String TrainSetFile = "TrainingData";

    private float steeringRightForce, steeringLeftForce, jumpingForce;
        
    private bool isJumping;
    
    public ControlMode controlMode;

    Vector3 initialPosition;
    Quaternion initialRotation;

    private static NeuralNet network;
    private SensorSuite sensors;

    private static List<DataSet> dataSets;

    private readonly static double[] LEFT_VECTOR = { 1.0, 0.0, 0.0 };
    private readonly double[] RIGHT_VECTOR = { 0.0, 1.0, 0.0 };
    private readonly static double[] JUMP_VECTOR = { 0.0, 0.0, 1.0 };


    // Use this for initialization
    void Start () {
        rigid = GetComponent<Rigidbody>();
        initialPosition = transform.position;
        initialRotation = transform.rotation;

        // 6 sensor inputs, 3 (commands) output
        network = new NeuralNet(6, 8, 3);
        sensors = GameObject.FindObjectOfType<SensorSuite>();
        dataSets = new List<DataSet>();

        if (controlMode == ControlMode.trainFromData)
        {
            LoadDataFromTrainSet();
            TrainNetwork();
        }

        if (controlMode == ControlMode.manualRecordForTraining)
        {
            Time.timeScale = 0.1f;
        }
    }

    private Command GetCommandFromOutput(double[] values)
    {
        int index = -1;
        double biggestValue = 0.0;

        for(int i = 0; i < values.Length; ++i)
        {
            if (biggestValue < values[i])
                index = i;
        }

        return (Command)index;
    }
    
    // Update is called once per frame
    void Update () {
        if (controlMode ==  ControlMode.automatic || controlMode == ControlMode.trainFromData)
        {
            double[] input = GetCurrentSensorVector();
            double[] output = network.Compute(input);

            Debug.Log(string.Format("Left: {0:F2}\tRight: {0:F2}\tJump: {0:F2}", output[0], output[1], output[2]));

            switch (GetCommandFromOutput(output))
            {
                case Command.Left:
                    {
                        if (output[(int)Command.Left] > 0.5)
                            SteerLeft(1, true);
                    }
                    break;
                case Command.Right:
                    {
                        if (output[(int)Command.Right] > 0.5)
                            SteerRight(1, true);
                    }
                    break;
                case Command.Jump:
                    {
                        if (output[(int)Command.Jump] > 0.5)
                        {
                            Jump(1, true);
                        }
                    }
                    break;
                default:
                    break;
            }
            return;
        }

        if (controlMode == ControlMode.manualRecordForTraining)
        {
            if (Input.GetKey(KeyCode.W))
                Time.timeScale = 1.0f;
            if (Input.GetKey(KeyCode.S))
                Time.timeScale = 0.05f;
        }

        if (Input.GetKey(KeyCode.A))
            SteerLeft(1, true);
        if (Input.GetKey(KeyCode.D))
            SteerRight(1,true);
        if (Input.GetKeyDown(KeyCode.Space))
            Jump(1,true);
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
        // Die letzten 10 Daten verwerfen
        if (controlMode == ControlMode.manualRecordForTraining)
        {
            if (dataSets.Count > 10)
                dataSets.RemoveRange(dataSets.Count - 10, 10);
            else
                dataSets.Clear();
        }

        StartCoroutine(Reset());
        SaveRecordedData();
    }

    public void SteerRight(float force, bool manualOverride)
    {
        if (controlMode == ControlMode.automatic && manualOverride)
            return;
        steeringRightForce = Mathf.Clamp01(force);

        if (controlMode == ControlMode.manualRecordForTraining)
            dataSets.Add(new DataSet(GetCurrentSensorVector(), RIGHT_VECTOR));
    }

    public void SteerLeft(float force, bool manualOverride)
    {
        if (controlMode == ControlMode.automatic && manualOverride)
            return;

        steeringLeftForce = Mathf.Clamp01(force);

        if (controlMode == ControlMode.manualRecordForTraining)
            dataSets.Add(new DataSet(GetCurrentSensorVector(), LEFT_VECTOR));
    }

    public void Jump(float force, bool manualOverride)
    {
        if (controlMode == ControlMode.automatic && manualOverride)
            return;
        jumpingForce = Mathf.Clamp01(force);

        if (controlMode == ControlMode.manualRecordForTraining)
            dataSets.Add(new DataSet(GetCurrentSensorVector(), JUMP_VECTOR));
    }
    public void StopSteering()
    {
        steeringRightForce=0;
        steeringLeftForce=0;
        jumpingForce=0;
    }

    private IEnumerator Reset()
    {
        yield return new WaitForSeconds(1);
        transform.position = initialPosition;
        transform.rotation = initialRotation;
        rigid.angularVelocity = Vector3.zero;
        rigid.velocity = Vector3.zero;
    }

    private void LoadDataFromTrainSet()
    {
        int lineCount = 0;
        string filepath = string.Format("./TrainingData/{0}.csv", TrainSetFile);
        System.IO.StreamReader file = new System.IO.StreamReader(filepath);
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
                        filepath,
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

            dataSets.Add(new DataSet(sensorValues, commandValues));
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
        if (dataSets.Count == 0)
            return;

        string path = "./TrainingData";

        if (!System.IO.Directory.Exists(path))
            System.IO.Directory.CreateDirectory(path);

        string filepath = string.Format("{0}/{1}-{2}.csv", path, TrainSetFile, DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss"));
        System.IO.StreamWriter file = new System.IO.StreamWriter(filepath);
        String line = String.Empty;

        foreach (var item in dataSets)
        {
            string values = GetCommaSeperatedStringFromVector(item.Values);
            string targets = GetCommaSeperatedStringFromVector(item.Targets);
            file.WriteLine("{0},{1}", values, targets);
            file.Flush();
        }

        file.Close();
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
        network.Train(dataSets, 0.05);
    }
}

public enum Command
{
    Left,
    Right,
    Jump
}

public enum ControlMode
{
    manual,
    manualRecordForTraining,
    automatic,
    trainFromData
};
