// See https://aka.ms/new-console-template for more information
using GoogleAdapter.Adapters;
using Mailer.Sender;
using MailerCli;
using MailerCommon.Configuration;
using MailerCommon.Configuration;
using Newtonsoft.Json;

Console.WriteLine("Mailer");

if (args.Length > 0 && args[0].EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
{
    PdfReader.Schedule sch = new PdfReader.Pdf().Read(args[0]);
    File.WriteAllText("./pdf.txt", sch.Logs);
    new PdfReader.ScheduleCsv().Convert(sch, "./pdf.csv");
    return;
}

//string googleApiServiceAccountSecretsJsonPath = args[0];
//string clmSendEmailsDocumentId = args[1];
//string googleApiOAuthSecretsJsonPath  = args[0];
string clmAssignmentListDocumentId = args[0];

string? sendGridApiKey = Environment.GetEnvironmentVariable("SENDGRID_API_KEY", EnvironmentVariableTarget.Process);
//string googleApiSecretsJson = File.ReadAllText(googleApiOAuthSecretsJsonPath);
string googleApiSecretsJson = File.ReadAllText("./GoogleApi.secrets.json");

// TODO: This is temporary, remove it
//clmSendEmailsDocumentId = clmAssignmentListDocumentId;

ISheets sheets = new GoogleSheets(googleApiSecretsJson);
ScheduleOptions options = new()
{
    EmailFromAddress = Environment.GetEnvironmentVariable("SENDGRID_API_KEY", EnvironmentVariableTarget.Process),
    EmailFromName = Environment.GetEnvironmentVariable("SENDGRID_API_KEY", EnvironmentVariableTarget.Process)
};

var scheduleOptions = new ScheduleOptions();

string documentsJson = File.ReadAllText("documents.json");

FullConfiguration? config = JsonConvert.DeserializeObject<FullConfiguration>(documentsJson) ;
var schedules = config.Schedules.Schedules;

new PublisherEmailer(
        config.Schedules, //new ScheduleOptions(),
        new ConsoleLogger<PublisherEmailer>(), 
        new DummyMemoryCache(), 
        sheets, 
        sendGridApiKey, 
        dryRunMode: true)
    .Run(
        utcNow: DateTime.UtcNow,
        friendInfoDocumentId: clmAssignmentListDocumentId,
        schedules: schedules.ToList());

//ISheets sheets = new CsvSheets();
//new PublisherEmailer(sheets, sendGridApiKey, dryRunMode: true, forceSendAll: true).Run(
//    clmSendEmailsDocumentId: @"D:\Downloads\Meeting Assignment Schedule and EMailer - CLM Send Emails.csv",
//    clmAssignmentListDocumentId: @"D:\Downloads\Meeting Assignment Schedule and EMailer - CLM Assignment List - Copy.csv",
//    pwSendEmailsDocumentId: @"D:\Downloads\Meeting Assignment Schedule and EMailer - PW Send Emails.csv",
//    pwAssignmentListDocumentId: @"D:\Downloads\Meeting Assignment Schedule and EMailer - PW Assignment List.csv",
//    mfsSendEmailsDocumentId: @"D:\Downloads\Meeting Assignment Schedule and EMailer - Service Send Emails.csv",
//    mfsAssignmentListDocumentId: @"D:\Downloads\Meeting Assignment Schedule and EMailer - Service Schedule.csv",
//    friendInfoDocumentId: @"D:\Downloads\Meeting Assignment Schedule and EMailer - Friend Info.csv");

//string template = File.ReadAllText("./clm-template.html");
//string output = new ClmScheduleGenerator()
//    .Generate(secretsJsonPath, documentId, range, friendName, template);

//File.WriteAllText(@"c:\Users\Marc\Desktop\clm.html", output);