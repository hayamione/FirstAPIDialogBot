using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder;
using System.Threading.Tasks;
using System.Threading;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder.Dialogs.Choices;
using System.Collections.Generic;

namespace NewDialogBot.Dialogs
{
    public class GetUserDetailsDialog : ComponentDialog
    {
        // Define value names for values tracked inside the dialogs.
        private const string UserInfo = "value-userInfo";

        public class DatePrompt : TextPrompt
        {
            public DatePrompt(string dialogId, PromptValidator<string> validator = null)
                : base(dialogId, validator)
            {
            }
        }

        public class PostalCodePrompt : NumberPrompt<int>
        {
            public PostalCodePrompt(string dialogId, PromptValidator<int> validator = null)
                : base(dialogId, validator)
            {
            }
        }

        public GetUserDetailsDialog() : base(nameof(GetUserDetailsDialog))
        {
            // This array defines how the Waterfall will execute.
            var waterfallSteps = new WaterfallStep[]
            {
                NameStepAsync,
                FamilyNameStepAsync,
                FullNameStepAsync,  
                BirthDateStepAsync,
                GenderPromptStepAsync,
                PostalCodeStepAsync,
                ExitStepAsync
                //SummaryStepAsync,
            };

            // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));

            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new NumberPrompt<int>(nameof(NumberPrompt<int>)));
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));

            AddDialog(new PostalCodePrompt(nameof(PostalCodePrompt), PostalCodePromptValidatorAsync));
            AddDialog(new DatePrompt(nameof(DatePrompt), DatePromptValidatorAsync));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> NameStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values[UserInfo] = new UserProfile();
            return await stepContext.PromptAsync(
                nameof(TextPrompt),
                new PromptOptions { Prompt = MessageFactory.Text("Let's get started. Please provide your first name.") },
                cancellationToken);
        }

        private async Task<DialogTurnResult> FamilyNameStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userProfile = (UserProfile)stepContext.Values[UserInfo];
            userProfile.Given = (string)stepContext.Result;
            return await stepContext.PromptAsync(
                nameof(TextPrompt),
                new PromptOptions { Prompt = MessageFactory.Text("Now, please provide your last / family name.") },
                cancellationToken);
        }

        private async Task<DialogTurnResult> FullNameStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userProfile = (UserProfile)stepContext.Values[UserInfo];
            userProfile.Family = (string)stepContext.Result;
            return await stepContext.PromptAsync(
                nameof(TextPrompt),
                new PromptOptions { Prompt = MessageFactory.Text("Great, Now, please provide your full name.") },
                cancellationToken);
        }

        private async Task<DialogTurnResult> BirthDateStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userProfile = (UserProfile)stepContext.Values[UserInfo];
            userProfile.Name = (string)stepContext.Result;
            return await stepContext.PromptAsync(
                nameof(DatePrompt),
                new PromptOptions { Prompt = MessageFactory.Text("Now, please provide your birth date."),
                RetryPrompt = MessageFactory.Text("The birthdate should be in this format (yyyy-mm-dd). Try again.")
                },
                cancellationToken);

        }

        private static async Task<DialogTurnResult> GenderPromptStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userProfile = (UserProfile)stepContext.Values[UserInfo];
            userProfile.BirthDate = (string)stepContext.Result;
            return await stepContext.PromptAsync(nameof(ChoicePrompt),
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Please select your gender"),
                    Choices = ChoiceFactory.ToChoices(new List<string> { "Male", "Female", "Other" }),
                }, cancellationToken);
        }

        private async Task<DialogTurnResult> PostalCodeStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userProfile = (UserProfile)stepContext.Values[UserInfo];
            userProfile.Gender = ((FoundChoice)stepContext.Result).Value;
            return await stepContext.PromptAsync(
                nameof(PostalCodePrompt),
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Lastly, please provide your postal code."),
                    RetryPrompt = MessageFactory.Text("The postal code must be of 5 digits. Try again."),
                },
                cancellationToken);
        }

        private async Task<DialogTurnResult> ExitStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userProfile = (UserProfile)stepContext.Values[UserInfo];
            userProfile.AddressPostalcode = (int)stepContext.Result;
            return await stepContext.EndDialogAsync(stepContext.Values[UserInfo], cancellationToken);
        }

        private Task<bool> PostalCodePromptValidatorAsync(PromptValidatorContext<int> promptContext, CancellationToken cancellationToken)
        {
            // Validate if the entered number is a valid Indian phone number
            return Task.FromResult(promptContext.Recognized.Succeeded && promptContext.Recognized.Value > 20000);
        }

        private Task<bool> DatePromptValidatorAsync(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            string datePattern = @"^\d{4}-\d{2}-\d{2}$";
            bool isValidDate = Regex.IsMatch(promptContext.Recognized.Value, datePattern);
            return Task.FromResult(isValidDate);
        }
    }
}
