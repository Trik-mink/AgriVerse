using System;
using System.Collections.Generic;
using UnityEngine;

namespace AgriVerse.Client
{
    [Serializable]
    public sealed class ConversationTurnDto
    {
        public string role = string.Empty;
        public string text = string.Empty;
    }

    [Serializable]
    public sealed class StakeholderConversation
    {
        public string stakeholder_id = string.Empty;
        public List<ConversationTurnDto> turns = new List<ConversationTurnDto>();
    }

    public sealed class InterviewNotebook
    {
        private readonly List<StakeholderConversation> conversations = new List<StakeholderConversation>();

        public InterviewNotebook(string scenarioId)
        {
            ScenarioId = scenarioId ?? string.Empty;
        }

        public string ScenarioId { get; }
        public IReadOnlyList<StakeholderConversation> Conversations => conversations;

        public IReadOnlyList<ConversationTurnDto> ConversationFor(string stakeholderId)
        {
            StakeholderConversation conversation = FindConversation(stakeholderId, false);
            return conversation == null ? Array.Empty<ConversationTurnDto>() : conversation.turns;
        }

        public void AddQuestion(string stakeholderId, string question)
        {
            AddTurn(stakeholderId, "student", question);
        }

        public void AddReply(string stakeholderId, string reply)
        {
            AddTurn(stakeholderId, "stakeholder", reply);
        }

        public bool HasResponse(string stakeholderId)
        {
            IReadOnlyList<ConversationTurnDto> turns = ConversationFor(stakeholderId);
            for (int index = 0; index < turns.Count; index++)
            {
                if (turns[index].role == "stakeholder")
                {
                    return true;
                }
            }

            return false;
        }

        public bool AreAllStakeholdersInterviewed(StakeholderDto[] stakeholders)
        {
            if (stakeholders == null || stakeholders.Length == 0)
            {
                return false;
            }

            for (int index = 0; index < stakeholders.Length; index++)
            {
                if (stakeholders[index] == null || !HasResponse(stakeholders[index].id))
                {
                    return false;
                }
            }

            return true;
        }

        private void AddTurn(string stakeholderId, string role, string text)
        {
            if (string.IsNullOrWhiteSpace(stakeholderId) || string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            FindConversation(stakeholderId, true).turns.Add(new ConversationTurnDto { role = role, text = text.Trim() });
        }

        private StakeholderConversation FindConversation(string stakeholderId, bool create)
        {
            for (int index = 0; index < conversations.Count; index++)
            {
                if (conversations[index].stakeholder_id == stakeholderId)
                {
                    return conversations[index];
                }
            }

            if (!create)
            {
                return null;
            }

            var conversation = new StakeholderConversation { stakeholder_id = stakeholderId };
            conversations.Add(conversation);
            return conversation;
        }
    }

    public sealed class InterviewNotebookSession : MonoBehaviour
    {
        private static InterviewNotebookSession instance;
        private InterviewNotebook notebook;

        public InterviewNotebook Notebook => notebook;

        public static InterviewNotebookSession GetOrCreate()
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<InterviewNotebookSession>();
            }

            if (instance == null)
            {
                instance = new GameObject("InterviewNotebookSession").AddComponent<InterviewNotebookSession>();
            }

            return instance;
        }

        public void ConfigureScenario(string scenarioId)
        {
            if (notebook == null || notebook.ScenarioId != scenarioId)
            {
                notebook = new InterviewNotebook(scenarioId);
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }
    }
}
