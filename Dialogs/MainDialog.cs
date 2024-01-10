using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using NewDialogBot;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NewDialogBot.Dialogs
{
    public class MainDialog : ComponentDialog
    {
        private readonly IStatePropertyAccessor<UserProfile> _userState;
        //private readonly JObject responses;

        public MainDialog(UserState userState)
            : base(nameof(MainDialog))
        {
            _userState = userState.CreateProperty<UserProfile>("UserProfile");

            // This array defines how the Waterfall will execute.
            var waterfallSteps = new WaterfallStep[]
            {
                WelcomeStepAsync,
                HelpStartAsync,
                SummarynApiStepAsync
            };

            // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));

            AddDialog(new GetUserDetailsDialog());

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> WelcomeStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var intentsJson = File.ReadAllText("Resources/intents.json");
            var intents = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(intentsJson);

            // Process user input
            var userInput = stepContext.Context.Activity.Text;

            // LUIS (CLU)

            //Intent - DownloadEMR
            //Entity - 
            //"EMR", "Medical Record", "Get my EMR", "Get my medical record", "I need my EMR document", "I need my emr", "I need my medical record",
            // "My name is Haya and am looking for my medical records and my gender is female."

            // Name, DOB, gender and zip code
            //YYYY-MM-DD My dob is if i rembeer correct 2000/09/09

            switch (userInput)
            {
                case "greetingintent":
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Hello! I'm your bot. Please type your request"), cancellationToken);
                    break;

                case var _ when intents["whoAreYou"].Contains(userInput):
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text($"I am Elite Bot. I display patient EMR."), cancellationToken);
                    break;

                case var _ when intents["help"].Contains(userInput):
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Please type 'EMR or I need EMR' If you want EMR."), cancellationToken);
                    //await stepContext.BeginDialogAsync(nameof(GetUserDetailsDialog), null, cancellationToken);
                    break;

                case var _ when intents["record"].Any(intent => userInput.Contains(intent)):
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text($"You entered EMR."), cancellationToken);
                    // If user requests EMR, begin the GetUserDetailsDialog
                    //await stepContext.BeginDialogAsync(nameof(GetUserDetailsDialog), null, cancellationToken);
                    break;

                default:
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Sorry, I did not recognize your request. Try again!"), cancellationToken);
                    break;
            }
            return await stepContext.EndDialogAsync(null, cancellationToken);
        }


        private async Task<DialogTurnResult> HelpStartAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.BeginDialogAsync(nameof(GetUserDetailsDialog), null, cancellationToken);
        }

        private async Task<DialogTurnResult> SummarynApiStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userInfo = (UserProfile)stepContext.Result;

            if (userInfo != null)
            {
                var summaryMessage = $"TThank you! Here is the summary:\n\nGiven: {userInfo.Given}\n\nFamily: {userInfo.Family}\n\nName: {userInfo.Name}\n\nBirthdate: {userInfo.BirthDate}\n\nGender: {userInfo.Gender}\n\nAddress Postal Code: {userInfo.AddressPostalcode}";               
                await stepContext.Context.SendActivityAsync(MessageFactory.Text(summaryMessage), cancellationToken);

                // Make API call and display PDF
                var apiResponse = await CallApiAsync(userInfo);
                if (apiResponse != null)
                {
                    // Display PDF document or handle it as needed
                    // Generate PDF and send it to the user
                    var pdfStream = new MemoryStream(apiResponse);
                    await SendPdfAsync(stepContext.Context, apiResponse);
                }
                else
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text("Failed to generate PDF, as API Response is null"), cancellationToken);
                }

                //var summaryMessage = $"We are in API Call Step in Waterfall";
                //await stepContext.Context.SendActivityAsync(MessageFactory.Text(summaryMessage), cancellationToken);

            }

            else
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("PDF generation canceled, as User Info is empty."), cancellationToken);
            }

            return await stepContext.EndDialogAsync(null, cancellationToken);
        }

        private async Task<byte[]> CallApiAsync(UserProfile userInfo)
        {
            using (var httpClient = new HttpClient())
            {
                var apiUrl = "https://elitedevutilities.azurewebsites.net/api/DevUtilities/DownloadEMR";

                // Modify payload to match the API requirements
                /*var payload = new
                {
                    Given = userInfo.Name,
                    Family = userInfo.Family,
                    Name = userInfo.Name,
                    BirthDate = userInfo.BirthDate,
                    Gender = userInfo.Gender,
                    AddressPostalcode = userInfo.AddressPostalcode
                };*/

                var payload = new
                {
                    Given = "Haya",
                    Family = "Ahmad",
                    Name = "Haya Zubair Ahmad",
                    BirthDate = "1952-02-09",
                    Gender = "Female",
                    AddressPostalcode = "22042"
                };

                var response = await httpClient.PostAsJsonAsync(apiUrl, payload);
                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsByteArrayAsync();
                    //File.WriteAllBytes("D:\workElite\bot-practice\GeneratedPdf.pdf", responseString);
                    Console.WriteLine($"API Response: {responseString}");
                    return responseString;
                }
                // Handle API call failure
                return null;
            }
        }

        private async Task SendPdfAsync(ITurnContext turnContext, byte[] pdfBytes)
        {
            if (pdfBytes != null && pdfBytes.Length > 0)
            {
                // Convert the byte array to a base64-encoded string
                string base64Pdf = Convert.ToBase64String(pdfBytes);

                // Create a Microsoft.Bot.Schema.Attachment with base64-encoded content
                var pdfAttachment = new Microsoft.Bot.Schema.Attachment
                {
                    Name = "GeneratedPdf.pdf",
                    ContentType = "application/pdf",
                    ContentUrl = "data:application/pdf;base64," + base64Pdf,
                };

                var reply = turnContext.Activity.CreateReply();
                reply.Attachments = new List<Microsoft.Bot.Schema.Attachment> { pdfAttachment };

                await turnContext.SendActivityAsync(reply);
            }
            else
            {
                await turnContext.SendActivityAsync(MessageFactory.Text("Failed to generate or retrieve the PDF content."), CancellationToken.None);
            }
        }





    }
}
