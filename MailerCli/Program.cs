// See https://aka.ms/new-console-template for more information
using Mailer.Sender;

Console.WriteLine("Mailer");

string googleApiServiceAccountSecretsJsonPath = args[0];
string clmSendEmailsDocumentId = args[1];
string googleApiOAuthSecretsJsonPath  = args[3];
string clmAssignmentListDocumentId = args[4];

string? sendGridApiKey = Environment.GetEnvironmentVariable("SENDGRID_API_KEY", EnvironmentVariableTarget.Process);
string googleApiSecretsJson = File.ReadAllText(googleApiOAuthSecretsJsonPath);

PublisherEmailer.Run(
    clmSendEmailsDocumentId: clmSendEmailsDocumentId,
    clmAssignmentListDocumentId: clmAssignmentListDocumentId, 
    sendGridApiKey: sendGridApiKey,
    googleApiSecretsJson: googleApiSecretsJson,
    dryRunMode: true);
        
//string template = File.ReadAllText("./template1.html");
//string output = new ClmScheduleGenerator()
//    .Generate(secretsJsonPath, documentId, range, friendName, template);

//File.WriteAllText(@"c:\Users\Marc\Desktop\template5.html", output);