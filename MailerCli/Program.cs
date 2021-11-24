// See https://aka.ms/new-console-template for more information
using GoogleAdapter.Adapters;
using MailerCommon;

Console.WriteLine("Mailer");

string secretsJsonPath = args[0];
string documentId = args[1];
string range = args[2];
string friendName = args[3];

string template = File.ReadAllText("./template1.html");

string output = new ClmScheduleGenerator()
    .Generate(secretsJsonPath, documentId, range, friendName, template);

File.WriteAllText(@"c:\Users\Marc\Desktop\template5.html", output);