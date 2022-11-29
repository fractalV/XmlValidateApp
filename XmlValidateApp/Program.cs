using Serilog;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

internal class Program
{
    private static void Main(string[] args)
    {
      
        ConfigureLogger(true);

        string xmlFileName, xsdFileName;

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        if (args.Length > 0 && args.Length < 3)
        {
            try
            {
                var document = new XmlDocument();
                var path = new Uri(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase)).LocalPath + "\\";
                xmlFileName = path + args[0];
                //xsdFileName = path + args[1];
                Log.Information($"Loading... {xmlFileName}");
                document.Load(xmlFileName);
                Log.Information($"Loaded {xmlFileName}");

                XmlReaderSettings settings = new XmlReaderSettings
                {
                    IgnoreComments = true,
                    ValidationType = ValidationType.Schema,
                    ValidationFlags = XmlSchemaValidationFlags.ReportValidationWarnings
                };

                settings.ValidationFlags |= XmlSchemaValidationFlags.ProcessSchemaLocation;
                settings.ValidationFlags |= XmlSchemaValidationFlags.ProcessIdentityConstraints;
                settings.ValidationFlags |= XmlSchemaValidationFlags.ProcessInlineSchema;

                settings.ValidationEventHandler += new ValidationEventHandler(ValEventHandler);

                XmlSchemaSet schemas = new XmlSchemaSet() { };

                string[] filePaths = Directory.GetFiles(path, "*.xsd");

                foreach (var item in filePaths)
                {
                    XDocument custOrdDoc = XDocument.Load(item);

                    if (custOrdDoc != null)
                    {
                        XElement root = custOrdDoc.Root;
                        var attrs = root.Attributes();
                        foreach (var attr in attrs)
                        {
                            if (attr.Name == "targetNamespace")
                            {                        
                                var fileName = item.Substring(item.LastIndexOf('\\') + 1);
                                document.Schemas.Add(attr.Value, fileName);
                                Log.Information($"Added namespace: {attr.Value} from {fileName}");
                            }
                        }
                    }
                }
                schemas.Compile();
             
                Log.Information("Validating...");

                document.Validate(ValEventHandler);               
               
                Log.Information("Validated.");
            }
            catch (Exception ex)
            {
                Log.Error($"{ex.Message}");
            }
        }
        else
        {
            Log.Warning("File name not specified");
            Console.WriteLine("Example: XmlValidateApp.exe xmlfile.xml");
        }

        static void ValEventHandler(object sender, ValidationEventArgs e)
        {
            if (e.Severity == XmlSeverityType.Warning)
            {
                Log.Warning(e.Message);
            }
            else if (e.Severity == XmlSeverityType.Error)
            {
                Log.Error(e.Message);

            } else { 
                Log.Information(e.Message); 
            }
           
        }

        static bool ConfigureLogger(bool bErrLog)
        {
            try
            {
                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Information()
                    .WriteTo.Console(outputTemplate: "[{Timestamp:dd-MM-yyyy HH:mm:ss:ms} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
                    .CreateLogger();
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
                return false;
            };
        }
    }
}