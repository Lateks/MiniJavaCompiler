using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MiniJavaCompilerTest.BackEnd
{
    [TestFixture]
    class CodeGenTest
    {
        string PEVerifyPath = @"C:\Program Files (x86)\Microsoft SDKs\Windows\v7.0A\Bin\PEVerify.exe";
        string compilerPath = @"MiniJavaCompiler\bin\Debug\MiniJavaCompiler.exe";
        string testCodePath = @"testcode";
        string testExePath =  @"test.exe";

        [TestFixtureSetUp]
        public void SetUp()
        {
            Directory.SetCurrentDirectory(@"..\..\..");
        }

        private System.Diagnostics.Process GetProcess(string programPath, string args = null)
        {
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            startInfo.FileName = programPath;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.UseShellExecute = false;
            if (args != null)
            {
                startInfo.Arguments = args;
            }
            process.StartInfo = startInfo;
            return process;
        }

        [Test]
        public void TestArraySum()
        {
            var compileProcess = GetProcess(compilerPath,
                String.Format("{0}\\{1} {2}", testCodePath, "arraysum.mjava", testExePath));
            compileProcess.Start();
            compileProcess.WaitForExit();
            Assert.AreEqual(0, compileProcess.ExitCode);
            var runProcess = GetProcess(testExePath);
            runProcess.Start();
            var output = runProcess.StandardOutput.ReadToEnd().Trim();
            Assert.AreEqual("45", output);

            CheckPEVerifyOutput();
        }

        private void CheckPEVerifyOutput()
        {
            var peverifyProcess = GetProcess(PEVerifyPath, testExePath);
            peverifyProcess.Start();
            var output = peverifyProcess.StandardOutput.ReadToEnd().Split(
                new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).Last();
            Assert.AreEqual(String.Format("All Classes and Methods in {0} Verified.", testExePath), output);
        }
    }
}
