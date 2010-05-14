using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Runtime.InteropServices;

public partial class frmMain : Form
{
    [DllImport("user32.dll")]
    static extern bool FlashWindow(IntPtr hwnd, bool bInvert);

    int MINLENGTH = 1;
    int MAXLENGTH = 1;
    //string DATAFILE = Path.Combine(Application.StartupPath, "combos.txt");
    int CurrentLength = 1;
    char[] Word = new char[1];
    StringBuilder SB = new StringBuilder("");
    //StreamWriter SW = null;
    Thread t = null;
    int[] CharsToUse = new int[1];


    //7DC3E82B35250CBC56139EF42ABDECED
    string SEARCHINGMD5 = "";
    string CURRENTMD5 = "";

    ulong PossibleCombos = 0;
    ulong AttemptsPerSecond = 0;
    ulong TotalAttempts = 0;
    ulong SecondsToComplete = 0;
    double TimeEstimate = 0;
    bool MatchFound = false;
    bool BackThreadRunning = false;
    string MessageToShow = "";

    public frmMain()
    {
        InitializeComponent();
    }

    private void btnStartResume_Click(object sender, EventArgs e)
    {
        //Validation
        if (txtMD5.Text.Trim().Length != 32)
        {
            ShowErrorBox("MD5 hashes must be 32 characters long and hexidecimal.");
            return;
        }
        if (int.TryParse(txtLowerLimit.Text, out MINLENGTH) == false ||
            int.TryParse(txtUpperLimit.Text, out MAXLENGTH) == false)
        {
            ShowErrorBox("Min and Max lengths must be valid integers.");
            return;
        }
        if (MINLENGTH > 16 || MAXLENGTH > 16)
        {
            ShowErrorBox("Min and Max lengths cannot be greater than 16.");
            return;
        }
        if (MINLENGTH > MAXLENGTH)
        {
            ShowErrorBox("Min length cannot be greater than Max length.");
            return;
        }
        if (chkLower.Checked == false && chkUpper.Checked == false &&
            chkNumeric.Checked == false && chkSpecial.Checked == false)
        {
            ShowErrorBox("You must select which characters set to use.");
            return;
        }

        //Valid. Do it!
        LockForm();        

        CreateCharacterArray();
        //7DC3E82B35250CBC56139EF42ABDECED
        //7D-C3-E8-2B-35-25-0C-BC-56-13-9E-F4-2A-BD-EC-ED
        SEARCHINGMD5 = "";
        string mashup = txtMD5.Text.Trim().ToUpper();
        for (int i = 0; i < mashup.Length; i += 2)
        {
            SEARCHINGMD5 += mashup.Substring(i, 2);
            SEARCHINGMD5 += "-";
        }
        SEARCHINGMD5 = SEARCHINGMD5.Substring(0, SEARCHINGMD5.Length - 1);
        
        BackThreadRunning = true;
        MatchFound = false;
        t = new Thread(BruteForceProcess);
        t.Start();
        //BruteForceProcess();
    }

    private void btnAbort_Click(object sender, EventArgs e)
    {
        t.Interrupt();
        BackThreadRunning = false;
        UnlockForm();
    }

    private void LockForm()
    {
        btnStartResume.Enabled = false;
        btnAbort.Enabled = true;
        btnExit.Enabled = false;
        txtResume.Enabled = false;
        txtMD5.Enabled = false;
        txtLastMD5.Enabled = false;
        txtLowerLimit.Enabled = false;
        txtUpperLimit.Enabled = false;
        chkLower.Enabled = false;
        chkUpper.Enabled = false;
        chkNumeric.Enabled = false;
        chkSpecial.Enabled = false;
        tmrMain.Enabled = true;
    }

    private void UnlockForm()
    {
        btnAbort.Enabled = false;
        btnStartResume.Enabled = true;
        btnExit.Enabled = true;
        txtResume.Enabled = true;
        txtMD5.Enabled = true;
        txtLastMD5.Enabled = true;
        txtLowerLimit.Enabled = true;
        txtUpperLimit.Enabled = true;
        chkLower.Enabled = true;
        chkUpper.Enabled = true;
        chkNumeric.Enabled = true;
        chkSpecial.Enabled = true;
        tmrMain.Enabled = false;
        lblStatus.Visible = false;
    }

    private void BruteForceProcess()
    {
        DateTime startTime = DateTime.Now;

        //Trash old data file...
        //if(File.Exists(DATAFILE))
        //    File.Delete(DATAFILE);

        //Create new data file and open up the file stream.
        try
        {
            SB = new StringBuilder();
            //SW = new StreamWriter(DATAFILE);

            TotalAttempts = 0;
            PossibleCombos = 0;
            for (int i = 1; i <= MAXLENGTH; i++)
            {
                PossibleCombos += (ulong)Math.Pow((double)CharsToUse.Length, (double)i);
            }
            PopulateCharArray(MINLENGTH);

            CurrentLength = MINLENGTH;
            for (int outerCount = MINLENGTH; outerCount <= MAXLENGTH; outerCount++)
            {
                CycleChar(0);

                if (BackThreadRunning == false)
                    break;

                //Increase word length
                CurrentLength++;
                PopulateCharArray(CurrentLength);
            }
        }
        catch(Exception ex)
        {
            BackThreadRunning = false;
            MessageToShow = "Error!\n" + ex.Message;
        }
        finally
        {
            //SW.Close();
        }

        DateTime endTime = DateTime.Now;
        TimeSpan ts = endTime - startTime;
        int secondsTaken = ts.Seconds;

        ////If we haven't aborted
        if (BackThreadRunning == true)
            MessageToShow = "No matching MD5 was found. Seconds Taken: " + secondsTaken.ToString();
        else if (BackThreadRunning == false && MatchFound == true)
            MessageToShow = "Matching MD5 found! Seconds Taken: " + secondsTaken.ToString();

        BackThreadRunning = false;
    }

    private void CreateCharacterArray()
    {
        int NumberOfChars = 0;
        int counter = 0;

        if (chkLower.Checked == true)
        {
            NumberOfChars += Program.Lowercase.Length;
        }
        if (chkUpper.Checked == true)
        {
            NumberOfChars += Program.Uppercase.Length;
        }
        if (chkNumeric.Checked == true)
        {
            NumberOfChars += Program.Numeric.Length;
        }
        if (chkSpecial.Checked == true)
        {
            NumberOfChars += Program.Special.Length;
        }

        CharsToUse = new int[NumberOfChars];

        if (chkLower.Checked == true)
        {
            foreach (int cha in Program.Lowercase)
            {
                CharsToUse[counter] = cha;
                counter++;
            }
        }
        if (chkUpper.Checked == true)
        {
            foreach (int cha in Program.Uppercase)
            {
                CharsToUse[counter] = cha;
                counter++;
            }
        }
        if (chkNumeric.Checked == true)
        {
            foreach (int cha in Program.Numeric)
            {
                CharsToUse[counter] = cha;
                counter++;
            }
        }
        if (chkSpecial.Checked == true)
        {
            foreach (int cha in Program.Special)
            {
                CharsToUse[counter] = cha;
                counter++;
            }
        }
    }

    private void CycleChar(int position)
    {
        if (BackThreadRunning == false)
            return;

        //End characters must be cycled each time a more starting character is cycled.
        for (int b = 0; b < CharsToUse.Length; b++)
        {
            Word[position] = (char)CharsToUse[b];
            if (position < (Word.Length - 1))
            {
                CycleChar(position + 1);
            }
            else
            {
                UseWord();
                if (MatchFound == true)
                {
                    break;
                }
            }
        }
    }

    private void UseWord()
    {
        //Write this word
        SB.Append(Word);
        CURRENTMD5 = EncodeMD5(SB.ToString());
        //SW.Write(SB.ToString() + "\t" + CURRENTMD5 + "\n");
        AttemptsPerSecond++;
        TotalAttempts++;

        if (CURRENTMD5 == SEARCHINGMD5)
        {
            MatchFound = true;
            BackThreadRunning = false;
            return;
        }

        SB = new StringBuilder();
    }

    private void PopulateCharArray(int size)
    {
        Word = new char[size];
        for (int i = 0; i < Word.Length; i++)
        {
            Word[i] = (char)CharsToUse[0];
        }
    }

    private void ResumeFromWord(string resumeWord)
    {

    }

    //EncodeMD5 Declarations
    //Declared once to help save processingtime.
    Byte[] originalBytes;
    Byte[] encodedBytes;
    MD5 md5 = new MD5CryptoServiceProvider();
    public string EncodeMD5(string originalString)
    {
        //Instantiate MD5CryptoServiceProvider, get bytes for original password and compute hash (encoded password)
        //md5 = new MD5CryptoServiceProvider();
        originalBytes = ASCIIEncoding.Default.GetBytes(originalString);
        encodedBytes = md5.ComputeHash(originalBytes);

        //Convert encoded bytes back to a 'readable' string
        return BitConverter.ToString(encodedBytes);//.Replace("-","");
    }

    private void tmrMain_Tick(object sender, EventArgs e)
    {
        lblStatus.Visible = true;

        try
        {
            txtResume.Text = SB.ToString();
            txtLastMD5.Text = CURRENTMD5.Replace("-","");
            if (MatchFound == true)
            {
                txtResume.Text = SB.ToString();
                txtLastMD5.Text = CURRENTMD5.Replace("-", "");
                UnlockForm();
                ShowMessageBox(MessageToShow);
                return;
            }
            else if (BackThreadRunning == false)
            {
                UnlockForm();
                ShowMessageBox(MessageToShow);
                return;
            }

            lblStatus.Text = "Attempts per second: " + AttemptsPerSecond.ToString();
            SecondsToComplete = (PossibleCombos - TotalAttempts) / AttemptsPerSecond;

            //Less than minute
            if (SecondsToComplete < 60)
            {
                lblStatus.Text += "\nTime to End: " + SecondsToComplete.ToString() + " seconds";
            }
            //Less than hour
            else if (SecondsToComplete < 3600)
            {
                TimeEstimate = (double)SecondsToComplete / 60;
                lblStatus.Text += "\nTime to End: " + TimeEstimate.ToString("0.00") + " minutes";
            }
            //Less than day
            else if (SecondsToComplete < 86400)
            {
                TimeEstimate = (double)SecondsToComplete / 3600;
                lblStatus.Text += "\nTime to End: " + TimeEstimate.ToString("0.00") + " hours";
            }
            //Less than a year
            else if (SecondsToComplete < 31536000)
            {
                TimeEstimate = (double)SecondsToComplete / 86400;
                lblStatus.Text += "\nTime to End: " + TimeEstimate.ToString("0.00") + " days";
            }
            //A year or more
            else
            {
                TimeEstimate = (double)SecondsToComplete / 31536000;
                lblStatus.Text += "\nTime to End: " + TimeEstimate.ToString("0.00") + " years";
            }

            AttemptsPerSecond = 0;
        }
        catch
        {
            lblStatus.Visible = false;
        }
    }

    private void ShowMessageBox(string msg)
    {
        if(this.Focused == false)
            FlashWindow(this.Handle, true);
        MessageBox.Show(this, msg, "MD5HashCrack Time Report", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void ShowErrorBox(string msg)
    {
        if (this.Focused == false)
            FlashWindow(this.Handle, true);
        MessageBox.Show(this, msg, "MD5HashCrack", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
    {
        if(t != null)
            t.Interrupt();
    }

    private void btnExit_Click(object sender, EventArgs e)
    {
        Application.Exit();
    }
}