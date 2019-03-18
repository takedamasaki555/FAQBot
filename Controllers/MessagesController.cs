using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Builder.Dialogs;
using System.Web.Http.Description;
using System.Net.Http;
using Microsoft.Bot.Builder.Dialogs.Internals;
using Autofac;
using System.Linq;
using System.Collections.Generic;
using FAQCategory;

namespace ChatBot
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        /// <summary>
        /// POST: api/Messages
        /// receive a message from a user and send replies
        /// </summary>
        /// <param name="activity"></param>
        [ResponseType(typeof(void))]
        public virtual async Task<HttpResponseMessage> Post([FromBody] Activity activity)
        {
            // check if activity is of type message
            if (activity != null && activity.GetActivityType() == ActivityTypes.Message)
            {
                await Conversation.SendAsync(activity, () => new Dialogs.RootDialog());
            }
            
            else if (activity != null && activity.GetActivityType() == ActivityTypes.ConversationUpdate)
            {
                IConversationUpdateActivity update = activity;
                using (var scope = DialogModule.BeginLifetimeScope(Conversation.Container, activity))
                {
                    var client = scope.Resolve<IConnectorClient>();
                    if (update.MembersAdded.Any())
                    {

                        foreach (var newMember in update.MembersAdded)
                        {
                            if (newMember.Id == activity.Recipient.Id)
                            {
                                Activity replyToConversation = activity.CreateReply("ようこそ！");
                                replyToConversation.Recipient = activity.From;
                                replyToConversation.Type = "message";
                                replyToConversation.Attachments = new List<Attachment>();
                                List<CardAction> cardButtons = new List<CardAction>();
                                CardAction cButton = new CardAction()
                                {
                                    Title = "ボットを呼ぶ",
                                    Type = ActionTypes.ImBack,
                                    Value = "ボットを呼ぶ"
                                };
                                cardButtons.Add(cButton);
                                
                                HeroCard plCard = new HeroCard()
                                {
                                    Title = "クリックして、ボクを呼んでね。",
                                    Buttons = cardButtons
                                };
                                Attachment plAttachment = plCard.ToAttachment();
                                replyToConversation.Attachments.Add(plAttachment);
                                await client.Conversations.ReplyToActivityAsync(replyToConversation);
                            }
                        }
                    }
                }
            }
            else
            {
                HandleSystemMessage(activity);
            }
            return new HttpResponseMessage(System.Net.HttpStatusCode.Accepted);
        }

        private Activity HandleSystemMessage(Activity message)
        {
            if (message.Type == ActivityTypes.DeleteUserData)
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (message.Type == ActivityTypes.ConversationUpdate)
            {
                // Handle conversation state changes, like members being added and removed
                // Use Activity.MembersAdded and Activity.MembersRemoved and Activity.Action for info
                // Not available in all channels
            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate)
            {
                // Handle add/remove from contact lists
                // Activity.From + Activity.Action represent what happened              
            }
            else if (message.Type == ActivityTypes.Typing)
            {
                // Handle knowing tha the user is typing
            }

            return null;
        }
    }
}