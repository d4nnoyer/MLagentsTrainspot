using System;
using System.ComponentModel;
using System.IO.Pipes;
using System.Security.Principal;
using System.Threading;
using Assets;
using TMPro;
using UnityEngine;

namespace UnityPipes
{

    public class SendAndReceive : MonoBehaviour
    {
        public MoveToGoalAgent agent;
        public GameObject textObject;
        private BackgroundWorker _backgroundWorker;
        private NamedPipeClientStream _client;
        private StreamString _clientStream;
        public string _inMessage;
        private bool _isLoggingEnabled = true;

        private void Start()
        {

            _client = new NamedPipeClientStream(".",
                                                "UnityPipe",
                                                PipeDirection.InOut,
                                                PipeOptions.None,
                                                TokenImpersonationLevel.None);
            try
            {
                _client.Connect();
            }
            catch (Exception)
            {
                UpdateTextBuffer("Error: connecting to pipe server failed.");
            }
            _clientStream = new StreamString(_client);

            _backgroundWorker = new BackgroundWorker();
            _backgroundWorker.DoWork += backgroundWorker_DoWork;
            _backgroundWorker.WorkerReportsProgress = true;
            _backgroundWorker.RunWorkerAsync();
        }


        private void OnDestroy()
        {
           
            try
            {
                if (_client.IsConnected) _client.Close(); ;
                _backgroundWorker.Dispose();
            }
            catch (Exception)
            {

                throw;
            }
        }

        private void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            while (!_client.IsConnected) Thread.Sleep(1000);
            while (true)
            {

                try
                {

                    _inMessage = _clientStream.ReadString();
                    if (_isLoggingEnabled) UpdateTextBuffer("Received Data from Server: " + _inMessage);
                    ProcessMessageFromServer(_inMessage);
                    _inMessage = "";
                    _client.Flush();

                    Thread.Sleep(30);
                }
                catch (Exception ex)
                {
                    UpdateTextBuffer(ex.Message + Environment.NewLine + ex.StackTrace + Environment.NewLine + "Last Message was: " + _inMessage);
                }
            }


        }

        public void UpdateTextBuffer(string text) => textObject.GetComponent<TextMeshProUGUI>().text += Environment.NewLine + text;

        private void ProcessMessageFromServer(string msg)
        {
            switch (msg.Split('?')[0])
            {
                case "cleanLogTextBox":
                    textObject.GetComponent<TextMeshProUGUI>().text = "";
                    break;

                case "toggleLogging":
                    _isLoggingEnabled = !_isLoggingEnabled;
                    textObject.GetComponent<TextMeshProUGUI>().text = "";
                    break;

                case "overrideNNmodel":
                    agent.OverrideOnnxModel(msg.Split('?')[1]);
                    this.UpdateTextBuffer("Overrided model!");

                    break;
            }

        }

    }
}