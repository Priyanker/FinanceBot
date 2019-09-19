using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using FinanceBot.Models;
using System;

namespace FinanceBot.Services
{
    public class BotStateService
    {
        #region Variables
        // State Variables
        public ConversationState ConversationState { get; }
        public UserState UserState { get; }

        // IDs
        public static string UserProfileId { get; } = $"{nameof(BotStateService)}.UserProfile";
        public static string ConversationDataId { get; } = $"{nameof(BotStateService)}.ConversationData";
        public static string DialogStateId { get; } = $"{nameof(BotStateService)}.DialogState";

        // Accessors
        public IStatePropertyAccessor<UserProfile> UserProfileAccessor { get; set; }
        public IStatePropertyAccessor<ConversationData> ConversationDataAccessor { get; set; }
        public IStatePropertyAccessor<DialogState> DialogStateAccessor { get; set; }
        #endregion

        public BotStateService(ConversationState conversationState, UserState userState)
        {
            ConversationState = conversationState ?? throw new ArgumentNullException(nameof(conversationState));
            UserState = userState ?? throw new ArgumentNullException(nameof(userState));

            InitializeAccessors();
        }

        public void InitializeAccessors()
        {
            // Initialize Conversation State Accessors
            ConversationDataAccessor = ConversationState.CreateProperty<ConversationData>(ConversationDataId);
            DialogStateAccessor = ConversationState.CreateProperty<DialogState>(DialogStateId);

            // Initialize User State
            UserProfileAccessor = UserState.CreateProperty<UserProfile>(UserProfileId);
        }
    }
}
