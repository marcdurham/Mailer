// See https://aka.ms/new-console-template for more information
using GoogleAdapter.Adapters;
using Mailer.Sender;
using MailerCli;
using MailerCommon;

Console.WriteLine("Mailer");

string googleApiServiceAccountSecretsJsonPath = args[0];
string clmSendEmailsDocumentId = args[1];
string googleApiOAuthSecretsJsonPath  = args[3];
string clmAssignmentListDocumentId = args[4];

string? sendGridApiKey = Environment.GetEnvironmentVariable("SENDGRID_API_KEY", EnvironmentVariableTarget.Process);
string googleApiSecretsJson = File.ReadAllText(googleApiOAuthSecretsJsonPath);

// TODO: This is temporary, remove it
clmSendEmailsDocumentId = clmAssignmentListDocumentId;

ISheets sheets = new GoogleSheets(googleApiSecretsJson);
new PublisherEmailer(new ConsoleLogger<PublisherEmailer>(), new DummyMemoryCache(), sheets, sendGridApiKey, dryRunMode: true).Run(
    clmSendEmailsDocumentId: clmSendEmailsDocumentId,
    clmAssignmentListDocumentId: clmAssignmentListDocumentId,
    pwSendEmailsDocumentId: clmSendEmailsDocumentId,
    pwAssignmentListDocumentId: clmAssignmentListDocumentId,
    mfsSendEmailsDocumentId: clmSendEmailsDocumentId,
    mfsAssignmentListDocumentId: clmAssignmentListDocumentId,
    friendInfoDocumentId: clmAssignmentListDocumentId,
    schedules: null);

//ISheets sheets = new CsvSheets();
//new PublisherEmailer(sheets, sendGridApiKey, dryRunMode: true, forceSendAll: true).Run(
//    clmSendEmailsDocumentId: @"D:\Downloads\Meeting Assignment Schedule and EMailer - CLM Send Emails.csv",
//    clmAssignmentListDocumentId: @"D:\Downloads\Meeting Assignment Schedule and EMailer - CLM Assignment List - Copy.csv",
//    pwSendEmailsDocumentId: @"D:\Downloads\Meeting Assignment Schedule and EMailer - PW Send Emails.csv",
//    pwAssignmentListDocumentId: @"D:\Downloads\Meeting Assignment Schedule and EMailer - PW Assignment List.csv",
//    mfsSendEmailsDocumentId: @"D:\Downloads\Meeting Assignment Schedule and EMailer - Service Send Emails.csv",
//    mfsAssignmentListDocumentId: @"D:\Downloads\Meeting Assignment Schedule and EMailer - Service Schedule.csv",
//    friendInfoDocumentId: @"D:\Downloads\Meeting Assignment Schedule and EMailer - Friend Info.csv");

//string template = File.ReadAllText("./template1.html");
//string output = new ClmScheduleGenerator()
//    .Generate(secretsJsonPath, documentId, range, friendName, template);

//File.WriteAllText(@"c:\Users\Marc\Desktop\template5.html", output);