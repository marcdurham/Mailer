// See https://aka.ms/new-console-template for more information
using GoogleAdapter.Adapters;

Console.WriteLine("Mailer");

string secretsJsonPath = args[0];
string documentId = args[1];
string range = args[2];

string json = File.ReadAllText(secretsJsonPath);

var sheets = new Sheets(json, isServiceAccount: true);

IList<IList<object>> values = sheets.Read(documentId: documentId, range: range);

string[] headers = new string[values[0].Count];
for (int i = 0; i < values[0].Count; i++)
{
    headers[i] = values[0][i].ToString();
}

string[] rows = new string[values.Count];
for (int i = 0; i < values.Count; i++)
{
    rows[i] = values[i][0].ToString();
}

for (int i = 1; i < values.Count; i++)
{

    for(int j = 0; j < values[i].Count; j++)
    {
        Console.WriteLine($"{rows[i]}:{headers[j]}:{values[i][j]}");
    }
    Console.WriteLine();
}