using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Assets.Scripts
{
    public class CsvTrainingLogger : ITrainReporter
    {
        private string logfile;
        public CsvTrainingLogger(String filename)
        {
            this.logfile = filename;

            if (File.Exists(logfile))
                File.Delete(logfile);

            writeToFile("Epoch,Error");

        }
        public void report(int epochs, double error)
        {
            string content = string.Format("\"{0}\",\"{1}\"", epochs, error);
            writeToFile(content);
        }

        private void writeToFile(String content)
        {
            File.AppendAllText(logfile, content + "\n");
        }
    }
}
