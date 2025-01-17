﻿using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using thps2patch;

namespace th2patchlauncher
{
    public partial class LauncherForm : Form
    {
        public LauncherForm()
        {
            InitializeComponent();
        }

        string curDir = Directory.GetCurrentDirectory();
        string patchPath = "patch";
        string levelsFile = "levelpatch.ini";
        string userFile = "userpatch.ini";
        string hawkFile = "THawk2.exe";
        string configFile = "th2_opt.cfg";
        string configFilePath => Path.Combine(curDir, configFile);
#pragma warning disable CS0414 // The field 'LauncherForm.patchName' is assigned but its value is never used
        string patchName = "dinput.dll";
#pragma warning restore CS0414 // The field 'LauncherForm.patchName' is assigned but its value is never used

        Options op;

        private void ParseCommandLineArgs()
        {
            string[] args = Environment.GetCommandLineArgs();

            try
            {
                if (args[1].ToUpper() == "-F")
                {
                    ShowInTaskbar = false;
                    WindowState = FormWindowState.Minimized;
                    Visible = false;
                    Hide();

                    LaunchTHPS(false);

                    Close();
                }
            }
            catch
            {
                //do nothing, we have no params
            }
        }


        private void Form1_Load(object sender, EventArgs e)
        {
            //load options from files
            op = new Options(configFilePath);

            //parse command line, if -F is there, apply patches and quit
            ParseCommandLineArgs();

            //if no args, setup UI
            UpdateControls();
        }


        enum DickSwap
        {
            none = 0,
            bam = 1,
            wolve = 2,
            lilper = 3,
            kor1 = 4,
            kor2 = 5,
            mcsqueeb = 6
        }


        private void UpdateControls()
        {
            gameBox.Text = op.GetString("Patch", "Game", "THPS2");
            op.Game = gameBox.Text;

            if (!File.Exists(Path.Combine(".\\", patchPath, levelsFile)))
            {
                op.Game = "THPS2";
                op.SetString("Patch", "Game", "THPS2");
                gameBox.Enabled = false;
            }

            userPatchBox.Checked = op.GetBool("Patch", "UserPatch", true);

            if (!File.Exists(Path.Combine(".\\", patchPath, userFile)))
            {
                op.UserPatch = false;
                userPatchBox.Enabled = false;
                op.SetBool("Patch", "UserPatch", false);
            }


            altSkinsBox.Checked = op.GetBool("Patch", "MoreSkins", false);
            swapBox.Enabled = altSkinsBox.Checked;

            switch (op.GetString("Patch", "DickSwap", "dick"))
            {
                case "bam": swapBox.SelectedIndex = 1; break;
                case "wolve": swapBox.SelectedIndex = 2; break;
                case "lilper": swapBox.SelectedIndex = 3; break;
                case "kor1": swapBox.SelectedIndex = 4; break;
                case "kor2": swapBox.SelectedIndex = 5; break;
                case "mcsqueeb": swapBox.SelectedIndex = 6; break;
                default: swapBox.SelectedIndex = 0; break;
            }


            skipIntroBox.Checked = op.GetBool("Patch", "SkipIntro", true);
            separateSaveBox.Checked = op.GetBool("Patch", "SeparateSaves", true);

            //patch tab

            switch (gameBox.Text.ToUpperInvariant())
            {
                case "THPS1": gameBox.SelectedIndex = 1; break;
                case "THPS3": gameBox.SelectedIndex = 2; break;
                case "THPS4": gameBox.SelectedIndex = 3; break;
                case "MHPB": gameBox.SelectedIndex = 4; break;
                default: gameBox.SelectedIndex = 0; break;
            }

            //loading fov value
            op.fovValueExist = op.GetFloat("Video", "FOV", 0);
            op.ZoomFactor = (int)(op.GetFloat("Video", "FOV", 1) * 100);

            op.ResX = op.GetInt("Video", "ResX", 1280);
            op.ResY = op.GetInt("Video", "ResY", 720);

            //video tab
            ResXbox.Text = op.ResX.ToString();
            ResYbox.Text = op.ResY.ToString();
            op.setResAspectText(resBox, aspectRatioDrop, op.ResolutionString);

            force32box.Checked = op.GetBool("Video", "Force32Bpp", false);
            unlockFPSbox.Checked = op.GetBool("Video", "UnlockFPS", false);
            overrideFOVbox.Checked = op.OverrideFOV;

            rendererBox.Checked = op.GetString("Video", "Renderer", "Hardware") == "Software" ? true : false;

            //just in case if file value is out of bounds
            op.ZoomFactor = op.ValidateRange(op.ZoomFactor, 30, 140);

            int fogValue = op.GetInt("Video", "FogScale", 300);
            fogSlider.Value = (int)Math.Sqrt((fogValue - 10) * fogSlider.Maximum);
            fogLabel.Text = fogValue.ToString();
            op.fog = fogValue;

            UpdateFOVbar();

            op.unfixResSelect = true;

            ambienceBox.Checked = op.GetBool("Music", "PlayAmbience", true);
            fadeBox.Checked = op.GetBool("Music", "Fade", true);
            randomBox.Checked = op.GetBool("Music", "Random", true);
            titleBox.Checked = op.GetBool("Music", "ShowTitle", true);
            separateTracksBox.Checked = op.GetBool("Music", "SeparateTracks", false);

            bigDropBox.Checked = op.GetBool("Input", "BigDrop", true);
            xinputBox.Checked = op.GetBool("Input", "XInput", true);
            vibrationBox.Checked = op.GetBool("Input", "Vibration", true);

            psxtexBox.Checked = op.GetBool("Video", "DisableNewtex", false);
            drawshadowBox.Checked = op.GetBool("Video", "DrawShadow", true);
            drawhudBox.Checked = op.GetBool("Video", "ShowHUD", true);

            skyBox.Checked = op.GetBool("Patch", "DisableSky", false);
            railBarBox.Checked = op.GetBool("Patch", "RailBalanceBar", true);

            manualsBox.Checked = op.GetBool("Input", "Manuals", true);

            this.Visible = true;
        }

        private void detectButtonClick(object sender, EventArgs e)
        {
            op.DetectResolution();
            op.setResAspectText(resBox, aspectRatioDrop, op.ResolutionString);

            MaybeUpdateFovBar(true);
        }



        public bool FileLocked(string filename)
        {
            FileStream stream = null;
            FileInfo file = new FileInfo(filename);

            try
            {
                stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None);
            }
            catch (IOException)
            {
                return true;
            }
            finally
            {
                if (stream != null) stream.Close();
            }

            return false;
        }

        //random value comparison
        public bool isSmallerExe()
        {
            bool result = false;

            using (var br = new BinaryReader(File.Open($"{curDir}\\{hawkFile}", FileMode.Open)))
            {
                if (br.BaseStream.Length >= 0x1642EC + 4)
                {
                    br.BaseStream.Position = 0x1642EC;

                    if (br.ReadUInt32() == 0xF92BD1F7)
                        result = true;
                }
            }

            return result;
        }

        public void LaunchTHPS(bool saveParams)
        {
            //wrong exe should lead to exit before trying to patch.
            if (!File.Exists("Thawk2.exe"))
            {
                MessageBox.Show(ErrorMsg.Thawk2NotFound, "Warning");
                return;
            }

            if (FileLocked("Thawk2.exe"))
            {
                MessageBox.Show(ErrorMsg.Thawk2InUse, "Warning");
                return;
            }

            if (!isSmallerExe())
            {
                MessageBox.Show(ErrorMsg.Thawk2NotSmaller, "Warning");
                return;
            }

            try
            {
                if (saveParams) op.Save();

                Process.Start($".\\{hawkFile}");

                var mp = new MainPatch(op);
                mp.Patch();

                Application.Exit();
            }
            catch (Exception ex)
            {
                MessageBox.Show("oops!\r\n\r\n" + ex.ToString(), ex.Message);
            }
        }

        private void launchButtonClick(object sender, EventArgs e)
        {
            LaunchTHPS(true);
        }

        private void quitButtonClick(object sender, EventArgs e)
        {
            op.Save();
            Application.Exit();
        }

        #region [General tab]

        private void altSkinsBox_CheckedChanged(object sender, EventArgs e)
        {
            op.SetBool("Patch", "MoreSkins", (sender as CheckBox).Checked);
            swapBox.Enabled = altSkinsBox.Checked;
        }

        private void swapBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            op.SetString("Patch", "DickSwap", ((DickSwap)((sender as ComboBox).SelectedIndex)).ToString());
        }

        private void skipIntroBox_CheckedChanged(object sender, EventArgs e)
        {
            op.SetBool("Patch", "SkipIntro", (sender as CheckBox).Checked);
        }

        private void separateSaveBox_CheckedChanged(object sender, EventArgs e)
        {
            op.SetBool("Patch", "SeparateSaves", (sender as CheckBox).Checked);
        }

        private void gameBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            op.SetString("Patch", "Game", (sender as ComboBox).Text);
        }

        #endregion

        #region [Video tab]

        private void rendererBox_CheckedChanged(object sender, EventArgs e)
        {
            bool softrender = (sender as CheckBox).Checked;

            op.SetString("Video", "Renderer", softrender ? "Software" : "Hardware");

            if (softrender)
                force32box.Checked = false;
        }

        private void fovSlider_Scroll(object sender, EventArgs e)
        {

        }

        private void trackBar1_ValueChanged(object sender, EventArgs e)
        {
            op.ZoomFactor = op.ValidateRange(fovSlider.Value, 30, 140);
            UpdateFOVbar();
        }

        private void fogSlider_ValueChanged(object sender, EventArgs e)
        {
            var fog = op.ValidateRange((int)(Math.Pow(fogSlider.Value, 2) / (float)fogSlider.Maximum + 10f), 10, 750);

            fogLabel.Text = fog.ToString();
            
            op.fog = fog;
        }

        private void fogSlider_MouseUp(object sender, MouseEventArgs e)
        {
            op.SetInt("Video", "FogScale", op.fog);
        }

        private void overrideFOVbox_CheckedChanged(object sender, EventArgs e)
        {
            fovSlider.Enabled = overrideFOVbox.Checked;

            MaybeUpdateFovBar(true);
        }

        private void UpdateFOVbar()
        {
            fovSlider.Value = op.ZoomFactor;
            fovLabel.Text = op.GetZoom().ToString("0.0##");

            op.SetFloat("Video", "FOV", op.GetZoom());
        }

        private void MaybeUpdateFovBar(bool forceAutoFOV)
        {
            if (!fovSlider.Enabled)
            {
                op.AutoFOV(forceAutoFOV);
                UpdateFOVbar();
            }
        }

        private void resBox_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            op.ParseResText(resBox.Text);

            //selecting other res doesn't update with autoFOV when false, this bool truns true after initiation
            MaybeUpdateFovBar(op.unfixResSelect);
        }

        private void unlockFPSbox_CheckedChanged(object sender, EventArgs e)
        {
            op.SetBool("Video", "UnlockFPS", (sender as CheckBox).Checked);
        }

        private void force32box_CheckedChanged(object sender, EventArgs e)
        {
            bool force32bits = (sender as CheckBox).Checked;

            op.SetBool("Video", "Force32Bpp", force32bits);

            if (force32bits)
                rendererBox.Checked = false;
        }

        private void aspectRatioDrop_SelectedIndexChanged(object sender, EventArgs e)
        {
            op.applyResolutionListByAspectRatio(resBox, aspectRatioDrop.Text);
        }

        private void resBox_SelectionChangeCommitted(object sender, EventArgs e)
        {
            op.ParseResText(resBox.Text);
            MaybeUpdateFovBar(true);
        }

        #endregion

        #region [Music tab]
        private void randomBox_CheckedChanged(object sender, EventArgs e)
        {
            op.SetBool("Music", "Random", (sender as CheckBox).Checked);
        }

        private void titleBox_CheckedChanged(object sender, EventArgs e)
        {
            op.SetBool("Music", "ShowTitle", (sender as CheckBox).Checked);
        }

        private void fadeBox_CheckedChanged(object sender, EventArgs e)
        {
            op.SetBool("Music", "Fade", (sender as CheckBox).Checked);
        }

        private void ambienceBox_CheckedChanged(object sender, EventArgs e)
        {
            op.SetBool("Music", "PlayAmbience", (sender as CheckBox).Checked);
        }

        private void separateTracksBox_CheckedChanged(object sender, EventArgs e)
        {
            op.SetBool("Music", "SeparateTracks", (sender as CheckBox).Checked);
        }
        #endregion

        #region [Patches tab]
        private void skyBox_CheckedChanged(object sender, EventArgs e)
        {
            op.SetBool("Patch", "DisableSky", (sender as CheckBox).Checked);
        }

        private void vibrationBox_CheckedChanged(object sender, EventArgs e)
        {
            op.SetBool("Input", "Vibration", (sender as CheckBox).Checked);
        }

        private void bigDropBox_CheckedChanged(object sender, EventArgs e)
        {
            op.SetBool("Input", "BigDrop", (sender as CheckBox).Checked);
        }

        private void xinputBox_CheckedChanged(object sender, EventArgs e)
        {
            op.SetBool("Input", "XInput", (sender as CheckBox).Checked);
        }

        private void userPatchBox_CheckedChanged(object sender, EventArgs e)
        {
            op.SetBool("Patch", "UserPatch", (sender as CheckBox).Checked);
        }

        private void manualsBox_CheckedChanged(object sender, EventArgs e)
        {
            op.SetBool("Input", "Manuals", (sender as CheckBox).Checked);
        }

        private void railBarBox_CheckedChanged(object sender, EventArgs e)
        {
            op.SetBool("Patch", "RailBalanceBar", (sender as CheckBox).Checked);
        }

        private void drawshadowBox_CheckedChanged(object sender, EventArgs e)
        {
            op.SetBool("Video", "DrawShadow", (sender as CheckBox).Checked);
        }

        private void drawhudBox_CheckedChanged(object sender, EventArgs e)
        {
            op.SetBool("Video", "ShowHUD", (sender as CheckBox).Checked);
        }

        private void psxtexBox_CheckedChanged(object sender, EventArgs e)
        {
            op.SetBool("Video", "DisableNewTex", (sender as CheckBox).Checked);
        }
        #endregion

        #region [About tab]
        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start((sender as LinkLabel).Text);
        }

        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start((sender as LinkLabel).Text);
        }

        private void linkLabel3_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start((sender as LinkLabel).Text);
        }
        #endregion

    }
}