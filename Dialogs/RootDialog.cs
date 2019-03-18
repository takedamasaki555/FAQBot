using System;
using System.Threading.Tasks;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Builder.Dialogs;
using System.Net.Http;
using System.Collections.Generic;
using CustomQnAMaker;
using Microsoft.Bot.Builder.CognitiveServices.QnAMaker;
using Newtonsoft.Json;

namespace ChatBot.Dialogs
{
    [Serializable]
    public class RootDialog : IDialog<object>
    {
        public string SelectedCategory = "";

        public async Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
        }

        public virtual async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> item)
        {
            PromptDialog.Choice(context, EnterQuestion, FAQCategory.FAQCategory.CategoryList, "カテゴリを選択してください。");
        }
       
        private async Task EnterQuestion(IDialogContext context, IAwaitable<string> item)
        {
            var message = await item;
            SelectedCategory = message;
            PromptDialog.Text(context, SearchQuestion, $"**質問を入力してください。**\n[{message}]が選択");
        }
        private async Task SearchQuestion(IDialogContext context, IAwaitable<string> item)
        {
            var message = await item;
            string StrictFilters = "";
            if (SelectedCategory != FAQCategory.FAQCategory.CategoryList[FAQCategory.FAQCategory.CategoryList.Count - 1])
            {
                StrictFilters = ", \"strictFilters\": [ { \"name\": \"category\", \"value\": \"" + SelectedCategory + "\"}]";
            }

            await context.PostAsync($"「{SelectedCategory}」で探しています...");

            string json = await GenerateAnswer.GetResultAsync(message, StrictFilters);
            if (json != "failture")
            {
                var result = JsonConvert.DeserializeObject<QnAMakerResults>(json);
                await ShowQuestions(context, result);
            }
        }

        private async Task ShowQuestions(IDialogContext context, QnAMakerResults result)
        {
            if (result.Answers.Count == 1 && result.Answers[0].Score == 0.0)
            {
                await context.PostAsync("あいにく見つかりませんでした。。。。");
                await AfterAnswerAsync(context, null);
            }
            else
            {
                int count = 0;
                var resultMessage = context.MakeMessage();
                resultMessage.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                resultMessage.Attachments = new List<Attachment>();
                for (int i = 0; i < result.Answers.Count; i++)
                {
                    for (int j = 0; j < result.Answers[i].Questions.Count; j++)
                    {
                        count++;
                        List<CardAction> cardButtons = new List<CardAction>();
                        CardAction cButton = new CardAction()
                        {
                            Title = "答えをみる",
                            Type = ActionTypes.ImBack,
                            Value = result.Answers[i].Questions[j]
                        };
                        cardButtons.Add(cButton);
                        HeroCard plCard = new HeroCard()
                        {
                            Text = result.Answers[i].Questions[j],
                            Buttons = cardButtons
                        };
                        resultMessage.Attachments.Add(plCard.ToAttachment());
                    }
                }
                CardAction lastButton = new CardAction()
                {
                    Title = "クリック",
                    Type = ActionTypes.ImBack,
                    Value = "上記のどれでもない"
                };
                List<CardAction> cardButton = new List<CardAction>();
                cardButton.Add(lastButton);
                HeroCard LastCard = new HeroCard()
                {
                    Text = "上記のどれでもない",
                    Buttons = cardButton
                };
                resultMessage.Attachments.Add(LastCard.ToAttachment());

                await context.PostAsync(resultMessage);
                context.Wait(ShowAnswer);
            }
        }

        private async Task ShowAnswer(IDialogContext context, IAwaitable<IMessageActivity> item)
        {
            var message = await item;

            if(message.Text == "上記のどれでもない")
            {
                await context.PostAsync("お役に立てず申し訳ございません。。");
                await AfterAnswerAsync(context, item);
            } else
            { 
                string json = await GenerateAnswer.GetResultAsync(message.Text, "");
                if (json != "failture")
                {
                    var result = JsonConvert.DeserializeObject<QnAMakerResults>(json);
                    await context.PostAsync(result.Answers[0].Answer.ToString());
                    await AfterAnswerAsync(context, item);
                }
            }
        }

        private async Task AfterAnswerAsync(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            List<string> Question = new List<string>();
            Question.Add("同じカテゴリで質問");
            Question.Add("カテゴリを変更");
            PromptDialog.Choice(context, ReenterQuestion, Question, "続けて質問できます。");
        }

        private async Task ReenterQuestion(IDialogContext context, IAwaitable<string> result)
        {
            var selectedMenu = await result;
            if (selectedMenu == "同じカテゴリで質問")
            {
                PromptDialog.Text(context, SearchQuestion, $"**質問を入力してください。**\n[{SelectedCategory}]が選択");
            }
            else if(selectedMenu == "カテゴリを変更")
            {
                PromptDialog.Choice(context, EnterQuestion, FAQCategory.FAQCategory.CategoryList, "カテゴリを選択してください。");
            }
        }
    }
}