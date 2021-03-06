﻿using System;
using System.Windows;
using System.Net.Sockets;

namespace xrFPServer
{
    /// <summary>
    /// Interaction logic for Verification.xaml
    /// </summary>
    public partial class Verification : Window, DPFP.Capture.EventHandler
    {
        private Data mydata;
        private NetworkStream stream;
        private DPFP.Verification.Verification Verificator;
        public Verification(Data data, NetworkStream Stream)
        {
            InitializeComponent();
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            mydata = data;
            stream = Stream;
        }

        protected void Process(DPFP.Sample Sample)
        {
            // Process the sample and create a feature set for the enrollment purpose.
            DPFP.FeatureSet features = ExtractFeatures(Sample, DPFP.Processing.DataPurpose.Verification);

            // Check quality of the sample and start verification if it's good
            // TODO: move to a separate task
            if (features != null)
            {
                // Compare the feature set with our template
                DPFP.Verification.Verification.Result result = new DPFP.Verification.Verification.Result();
                // Compare feature set with all stored templates.
                for (int i = 0; i < mydata.serialNum; i++)
                {
                    Verificator.Verify(features, mydata.Templates[i], ref result);
                    mydata.IsFeatureSetMatched = result.Verified;
                    UpdateStatus(result.FARAchieved);
                    mydata.FalseAcceptRate = result.FARAchieved;
                    if (result.Verified)
                    {
                        MakeReport("The fingerprint was VERIFIED.");
                        string msg = "1" + string.Format("{0:00}", mydata.serialName[i].Length) + mydata.serialName[i];
                        byte[] msg2 = System.Text.Encoding.ASCII.GetBytes(msg);
                        stream.Write(msg2, 0, msg2.Length);
                        this.Close();
                        break;
                    }
                }
                if (!result.Verified)
                {
                    MakeReport("The fingerprint was NOT VERIFIED.");
                    byte[] msg2 = System.Text.Encoding.ASCII.GetBytes("0");
                    stream.Write(msg2, 0, msg2.Length);
                }

                mydata.Update();
            }
        }

        private void UpdateStatus(int FAR)
        {
            // Show "False accept rate" value
            SetStatus(String.Format("False Accept Rate (FAR) = {0}", FAR));
        }

        protected void Init()
        {
            this.closeAndSaveButton.IsEnabled = false;
            this.statusTextBox.Clear();
            Verificator = new DPFP.Verification.Verification();     // Create a fingerprint template verificator
            UpdateStatus(0);
            try
            {
                Capturer = new DPFP.Capture.Capture();				// Create a capture operation.

                if (null != Capturer)
                    Capturer.EventHandler = this;					// Subscribe for capturing events.
                else
                    SetPrompt("Can't initiate capture operation!");
            }
            catch
            {
                MessageBox.Show("Can't initiate capture operation!", "Error", MessageBoxButton.OK);
            }
        }

        protected void Start()
        {
            if (null != Capturer)
            {
                try
                {
                    Capturer.StartCapture();
                    SetPrompt("Using the fingerprint reader, scan your fingerprint.");
                }
                catch
                {
                    SetPrompt("Can't initiate capture!");
                }
            }
        }

        protected void Stop()
        {
            if (null != Capturer)
            {
                try
                {
                    Capturer.StopCapture();
                }
                catch
                {
                    SetPrompt("Can't terminate capture!");
                }
            }
        }

        #region Form Event Handlers:
        private void VerificationWindow_Closed(object sender, EventArgs e)
        {
            Stop();
            byte[] msg = System.Text.Encoding.ASCII.GetBytes("0");
            stream.Write(msg, 0, msg.Length);
        }

        private void VerificationWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Init();
            Start();
        }

        private void CloseAndSaveButton_Click(object sender, RoutedEventArgs e)
        {

        }
        #endregion

        #region EventHandler Members:

        public void OnComplete(object Capture, string ReaderSerialNumber, DPFP.Sample Sample)
        {
            MakeReport("The fingerprint sample was captured.");
            SetPrompt("Scan the same fingerprint again.");
            Process(Sample);
        }

        public void OnFingerGone(object Capture, string ReaderSerialNumber)
        {
            MakeReport("The finger was removed from the fingerprint reader.");
        }

        public void OnFingerTouch(object Capture, string ReaderSerialNumber)
        {
            MakeReport("The fingerprint reader was touched.");
        }

        public void OnReaderConnect(object Capture, string ReaderSerialNumber)
        {
            MakeReport("The fingerprint reader was connected.");
        }

        public void OnReaderDisconnect(object Capture, string ReaderSerialNumber)
        {
            MakeReport("The fingerprint reader was disconnected.");
        }

        public void OnSampleQuality(object Capture, string ReaderSerialNumber, DPFP.Capture.CaptureFeedback CaptureFeedback)
        {
            if (CaptureFeedback == DPFP.Capture.CaptureFeedback.Good)
                MakeReport("The quality of the fingerprint sample is good.");
            else
                MakeReport("The quality of the fingerprint sample is poor.");
        }
        #endregion

        protected DPFP.FeatureSet ExtractFeatures(DPFP.Sample Sample, DPFP.Processing.DataPurpose Purpose)
        {
            DPFP.Processing.FeatureExtraction Extractor = new DPFP.Processing.FeatureExtraction();  // Create a feature extractor
            DPFP.Capture.CaptureFeedback feedback = DPFP.Capture.CaptureFeedback.None;
            DPFP.FeatureSet features = new DPFP.FeatureSet();
            Extractor.CreateFeatureSet(Sample, Purpose, ref feedback, ref features);            // TODO: return features as a result?
            if (feedback == DPFP.Capture.CaptureFeedback.Good)
                return features;
            else
                return null;
        }

        protected void SetStatus(string status)
        {
            this.Dispatcher.Invoke(new Action(delegate () {
                statusLabel.Content = status;
            }));
        }

        protected void SetPrompt(string prompt)
        {
            this.Dispatcher.Invoke(new Action(delegate () {
                promptTextBox.Text = prompt;
                if (prompt == "Click Close, and then click Fingerprint Verification.")
                {
                    this.closeAndSaveButton.IsEnabled = true;
                    CloseAndSaveButton_Click(null, null);
                }
            }));
        }
        protected void MakeReport(string message)
        {
            this.Dispatcher.Invoke(new Action(delegate () {
                statusTextBox.AppendText(message + "\r\n");
            }));
        }

        private DPFP.Capture.Capture Capturer;

       
    }
}
