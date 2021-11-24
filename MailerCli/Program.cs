// See https://aka.ms/new-console-template for more information
using GoogleAdapter.Adapters;
using Mailer.Sender;
using MailerCommon;

Console.WriteLine("Mailer");

string secretsJsonPath = args[0];
string clmSendEmailsDocumentId = args[1];
string range = args[2];
string googleApiSecretsJsonPath = args[3];
string clmAssignmentListDocumentId = args[4];

string? sendGridApiKey = Environment.GetEnvironmentVariable("SENDGRID_API_KEY", EnvironmentVariableTarget.Process);
string googleApiSecretsJson = File.ReadAllText(googleApiSecretsJsonPath);

PublisherEmailer.Run(
    clmSendEmailsDocumentId: clmSendEmailsDocumentId,
    clmAssignmentListDocumentId: clmAssignmentListDocumentId, 
    range: range,
    sendGridApiKey: sendGridApiKey,
    googleApiSecretsJson: googleApiSecretsJson);
        
//string template = File.ReadAllText("./template1.html");
//string output = new ClmScheduleGenerator()
//    .Generate(secretsJsonPath, documentId, range, friendName, template);

//File.WriteAllText(@"c:\Users\Marc\Desktop\template5.html", output);