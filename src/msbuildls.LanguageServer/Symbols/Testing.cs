using System.IO;
using System.Xml;
using Microsoft.Build.Definition;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Framework.Profiler;

namespace Testing;

public class Test
{
    public void Testing()
    {
        var project = Microsoft.Build.Evaluation.Project.FromFile("", new ProjectOptions());
        Project.FromXmlReader(XmlReader.Create(new StringReader("")), new ProjectOptions() { });
    }
}