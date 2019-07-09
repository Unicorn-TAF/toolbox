using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace Unicorn.TestAdapter
{
    internal class ExecutorUtilities
    {
        internal static string PrepareRunDirectory(string baseDir)
        {
            var runDir = Path.Combine(baseDir, "TestResults", $"{Environment.MachineName}_{DateTime.Now.ToString("MM-dd-yyyy_hh-mm")}");

            if (!Directory.Exists(runDir))
            {
                Directory.CreateDirectory(runDir);
            }

            return runDir;
        }

        internal static void CopySourceFilesToRunDir(string sourceDir, string targetDir)
        {
            foreach (var source in Directory.GetFiles(sourceDir))
            {
                File.Copy(source, source.Replace(sourceDir, targetDir), true);
            }
        }

        internal static void CopyDeploymentItems(IRunContext runContext, string runDir, IFrameworkHandle frameworkHandle)
        {
            var runSettingsXml = XDocument.Parse(runContext.RunSettings.SettingsXml);

            var msTestElement = runSettingsXml
                .Element("RunSettings")
                .Element("MSTest");

            if (msTestElement == null)
            {
                return;
            }

            var testSettingsPath = msTestElement
                .Element("SettingsFile")
                .Value;

            frameworkHandle.SendMessage(TestMessageLevel.Informational, "Test Settings: " + testSettingsPath);

            var testSettingsXml = XDocument.Load(testSettingsPath);

            XNamespace nsa = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";

            var deploymentItems = testSettingsXml
                .Element(nsa + "TestSettings")
                .Element(nsa + "Deployment")
                .Elements(nsa + "DeploymentItem")
                .Select(d => d.Attribute("filename").Value);

            frameworkHandle.SendMessage(TestMessageLevel.Informational, "Deployment Items: " + string.Join(",", deploymentItems));

            foreach (var deploymentItem in deploymentItems)
            {
                var item = Path.IsPathRooted(deploymentItem) ?
                    deploymentItem :
                    Path.Combine(runContext.SolutionDirectory, deploymentItem);

                var itemDirectory = Path.GetDirectoryName(item);

                var itemAttributes = File.GetAttributes(item);

                if (itemAttributes.HasFlag(FileAttributes.Directory))
                {
                    CopySourceFilesToRunDir(itemDirectory, runDir);
                }
                else
                {
                    File.Copy(item, item.Replace(itemDirectory, runDir), true);
                }
            }
        }
    }
}
