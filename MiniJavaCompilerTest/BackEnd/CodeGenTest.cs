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
        string PEVerifyPath = @"C:\Program Files (x86)\Microsoft SDKs\Windows\v8.0A\bin\NETFX 4.0 Tools\x64\PEVerify.exe";
        string compilerPath = @"..\..\..\MiniJavaCompiler\bin\Debug\MiniJavaCompiler.exe";
        string testCodePath = @"..\..\..\testcode";
        string testExePath =  @"..\..\..\test.exe";

        [Test]
        public void TestArraySum()
        {
            CheckCompilationOK("arraysum.mjava");
            CheckSingleLineOutput("45");
            CheckPEVerifyOutput();
        }

        [Test]
        public void TestArrayPolymorphism()
        {
            CheckCompilationOK("arraytest.mjava");
            CheckSingleLineOutput(""); // no output (if no assertion errors occurred)
            CheckPEVerifyOutput();
        }

        [Test]
        public void TestBooleans()
        {
            CheckCompilationOK("booleans.mjava");
            CheckMultiLineOutput(new string[] { "1", "0", "1", "0", "1", "0", "1", "0", "1", "0" });
            CheckPEVerifyOutput();
        }

        [Test]
        public void TestErrors()
        {
            var compileProcess = GetProcess(compilerPath, FormatParams("errortest.mjava"));
            compileProcess.Start();
            var output = compileProcess.StandardOutput.ReadToEnd()
                .Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            Assert.AreEqual(1, compileProcess.ExitCode);
            Assert.AreEqual(3 * 20 + 1, output.Count()); // 21 errors with two code output lines for each of them
        }

        [Test]
        public void TestFactorial()
        {
            CheckCompilationOK("factorial.mjava");
            CheckSingleLineOutput("3628800"); // 10! = 3628800
            CheckPEVerifyOutput();
        }

        [Test]
        public void TestClassFields()
        {
            CheckCompilationOK("fieldtest.mjava");
            CheckSingleLineOutput("0"); // prints 0 if class int fields are initialized to 0 by default
            CheckPEVerifyOutput();
        }

        [Test]
        public void TestInheritanceAndPolymorphism()
        {
            CheckCompilationOK("inheritance.mjava");
            CheckMultiLineOutput(new string[] { "0", "5555", "42", "0" });
            CheckPEVerifyOutput();
        }

        [Test]
        public void TestMethodInvocationsWithArguments()
        {
            CheckCompilationOK("methodinvocation.mjava");
            CheckMultiLineOutput(new string[] { "8", "4", "7", "13" });
            CheckPEVerifyOutput();
        }

        [Test]
        public void TestOpcodeGeneration() // Note: this does not check the actual opcodes produced.
        {
            CheckCompilationOK("opcode_test.mjava");
            CheckMultiLineOutput(new string[] { "0", "1", "2", "3", "4", "5", "6", "7",
                "8", "9", "255", "256", "1000", "1", "2", "3", "4", "5",
                "6", "7", "1", "2", "3", "4", "5", "0", "0" });
            CheckPEVerifyOutput();
        }

        [Test]
        public void TestBooleanOperatorShortCircuiting()
        {
            CheckCompilationOK("shortcircuit.mjava");
            CheckMultiLineOutput(new string[] { "1", "1",
                "Exception occurred: AssertionError at Test2.makeAssertion(31,5)" });
            CheckPEVerifyOutput();
        }

        [Test]
        public void TestIfStatement()
        {
            CheckCompilationOK("trivial.mjava");
            CheckSingleLineOutput("1");
            CheckPEVerifyOutput();
        }

        [Test]
        public void TestVariableNameHiding()
        {
            CheckCompilationOK("variablenamehiding.mjava");
            CheckMultiLineOutput(new string[] { "1", "1", "2", "0", "2", "2", "1", "3", "10",
                "Exception occurred: AssertionError at Test.main(20,5)" });
            CheckPEVerifyOutput();
        }

        private void CheckSingleLineOutput(string expected)
        {
            Assert.AreEqual(expected, GetOutput().Trim());
        }

        private void CheckMultiLineOutput(string[] expected)
        {
            var output = GetOutput().Split(new string[] { Environment.NewLine },
                StringSplitOptions.RemoveEmptyEntries);
            CollectionAssert.AreEqual(expected, output);
        }

        private string GetOutput()
        {
            var runProcess = GetProcess(testExePath);
            runProcess.Start();
            return runProcess.StandardOutput.ReadToEnd();
        }

        private void CheckCompilationOK(string inputFileName)
        {
            var compileProcess = GetProcess(compilerPath, FormatParams(inputFileName));
            compileProcess.Start();
            compileProcess.WaitForExit();
            Assert.AreEqual(0, compileProcess.ExitCode);
        }

        private string FormatParams(string inputFileName)
        {
            return String.Format("{0}\\{1} {2}", testCodePath, inputFileName, testExePath);
        }

        private void CheckPEVerifyOutput()
        {
            var peverifyProcess = GetProcess(PEVerifyPath, testExePath);
            peverifyProcess.Start();
            var output = peverifyProcess.StandardOutput.ReadToEnd().Split(
                new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).Last();
            Assert.AreEqual(String.Format("All Classes and Methods in {0} Verified.", testExePath), output);
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
    }
}
