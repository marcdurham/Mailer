using Adapters;
using GoogleAdapter.Adapters;
using SendGrid;
using System.Text.RegularExpressions;

namespace Mailer.Sender;
public class PublisherEmailer 
{
    public void Run(string[] args)
    {
        Console.WriteLine("Reading and writing to a Google spreadsheet...");
        string secretsJsonPath = args[0];
        string documentId = args[1];
        string range = args[2];
        string targetDocumentId = args[3];
        string targetRange = args[4];
        string sendGridApiKey = args[5];

        string json = File.ReadAllText(secretsJsonPath);

        var sheets = new Sheets(json, isServiceAccount: true);

        IList<IList<object>> rows = sheets.Read(documentId: documentId, range: range);

        //var values = new[] { new[] { (object)DateTime.Now.ToLongTimeString(), (object)"Azure Function" } };

        //sheets.Write(
        //    documentId: targetDocumentId,
        //    range: targetRange,
        //    values: values);

        var publishers = new List<PublisherClass>();
        foreach (var r in rows)
        {
            string? sent = r.Count > 2 ? $"{r[2]}" : null;
            publishers.Add(
                new PublisherClass
                {
                    Name = $"{r[0]}",
                    Email = $"{r[1]}",
                    Sent = $"{sent}"
                });
        }


        foreach (PublisherClass publisher in publishers)
        {
            Console.WriteLine($"Sending email to {publisher.Name}: {publisher.Email}: {publisher.Sent}...");
            // Email // Response response = SendGridEmailer.SendEmail(name, email, sendGridApiKey).Result;
            // Email // Console.WriteLine($"Status Code:{response.StatusCode}");

            if (string.IsNullOrWhiteSpace(publisher.Name))
                break; // End of list

            publisher.Sent = DateTime.Now.ToString();

            string emailPattern = @"^\S+@\S+$";
            if (Regex.IsMatch(publisher.Email, emailPattern))
            {
                publisher.Sent = "Sending";
                if (publisher.Email.ToUpper().EndsWith("@gmail.com"))
                {

                }
                else
                {
                    try
                    {
                        Response response = SendGridEmailer.SendEmail(publisher.Name, publisher.Email, sendGridApiKey).Result;
                        Console.WriteLine($"Sent Status Code:{response.StatusCode}");
                    }
                    catch (Exception ex)
                    {
                        publisher.Result = ex.Message;
                    }
                }
            }
            else
            {
                publisher.Result = $"FAIL: not a valid email address";
            }


            //string cellToWrite = Sheets.NextCellDown(range, "D", publishers.IndexOf(publisher));

            // This will use up the write reqeusts per minute per user too fast
            //Console.WriteLine($"   Writing to: {cellToWrite}");
            //sheets.WriteOneCell(
            //    documentId: documentId,
            //    range: cellToWrite,
            //    value: "Done2");

            //IList<IList<object>> newValues= sheets.Read(documentId: documentId, range: range);
        }


        Console.WriteLine("Writing new values back");
        foreach (PublisherClass publisher in publishers)
        {
            rows[publishers.IndexOf(publisher)] = new object[4] { 
                publisher.Name, 
                publisher.Email, 
                publisher.Sent, 
                publisher.Result };
        }

        sheets.Write(
            documentId: documentId,
            range: range,
            values: rows);


    }


//IList<IList<object>> linesToEdit = tester.Read(documentId: documentId, range: range);

//for (int r = 4; r < 7; r++)
//{
//    linesToEdit[r][2] = "2/3/1111";
//}

//var oValues = new[] { new[] { (object)"4/4/4444" } };

//tester.Write(
//    documentId: documentId,
//    range: range,
//    values: oValues); 

//IList<IList<object>> lines = tester.Read(documentId: documentId, range: range);

//foreach(IList<object> line in lines)
//{
//    foreach(object value in line)
//    {
//        Console.Write($"{value}, ");
//    }
//    Console.WriteLine();
//}


}

public class PublisherClass
{
    public string Name { get; set; }
    public string Email { get; set; }
    public string Sent { get; set; }
    public string Result { get; set; }
}