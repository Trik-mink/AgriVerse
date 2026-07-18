using System;

namespace AgriVerse.Client
{
    /// <summary>
    /// Scenario-specific authored presentation configuration mirrored verbatim from
    /// docs/salt-line-script.md. Reusable controllers consume this configuration and
    /// do not branch on scenario IDs.
    /// </summary>
    public static class SaltLineNarrative
    {
        public const string Title = "AgriVerse";
        public const string Episode = "Episode 1: The Salt Line";
        public const string Tagline = "Test the water. Hear the people. Revise the future.";
        public const string Intro =
            "A farming community in Vietnam's Mekong Delta is losing its rice to something it cannot see. " +
            "You are the field advisor sent to find out why - and to leave behind a plan that works. " +
            "The district council meets after tomorrow's tide. What you learn before then is up to you.";
        public const string NamePrompt = "What should the community call you?";
        public const string StartButton = "Begin the field mission";

        public const string ArrivalTemplate =
            "[PLAYER]? Good - you're here. I'm Mai, your field coordinator. Something is wrong " +
            "with the water, and nobody agrees on what to do about it. The district council meets " +
            "after tomorrow's tide, and they expect a plan grounded in evidence. Let's start where " +
            "every good plan starts: with the water itself.";

        public const string InvestigationIntro =
            "Three plots, three stories. Test each one and your notebook will keep the record. " +
            "Before each reading - tell me what you expect. Good advisors predict, then measure.";
        public const string AfterReading =
            "Now the numbers are yours. The notebook remembers.";
        public const string AfterAllReadings =
            "Three plots, three different worlds - and the same delta. Numbers alone will not " +
            "finish this plan. Time to hear the people who live with this water.";
        public const string InterviewIntro =
            "Meet all three - the farmer, the researcher, the official. They disagree, and every " +
            "one of them is right about something. Ask real questions. Your plan will have to " +
            "answer to each of them.";
        public const string AfterAllInterviews =
            "You have heard the field, the science, and the district. Ready to put a plan on the table?";
        public const string PlanningIntro =
            "Build your proposal. Match the intervention to what you measured - and to what you " +
            "heard. If your plan needs money the farmer does not have, say who will carry that cost. " +
            "The council will ask.";
        public const string ConsequencesIntro =
            "The model will now play your plan forward five years. Walk through them slowly. " +
            "The delta answers honestly.";
        public const string FeedbackIntro =
            "The review is grounded in the evidence - every claim is cited. Read what your plan " +
            "does well, and what it missed. No advisor gets it perfect the first time.";
        public const string RevisionIntro =
            "This is the real work of an advisor: take what you learned and make the plan stronger. " +
            "The community is counting on the second draft.";
        public const string ImprovedResult =
            "Look at the difference. That is what revision does - not an admission of failure, " +
            "but the way good plans are made.";
        public const string BriefIntro =
            "Your findings, your interviews, your plan - written up for the council. " +
            "This is what you leave behind.";
        public const string EndingTemplate =
            "The council has your brief, [PLAYER]. Your mission here is done - you have earned " +
            "the journey home. Or stay another season, if the delta has more to teach you. " +
            "Either way: this community plans its future with your work in its hands.";

        public const string ReturnHome = "Return home";
        public const string StayAnotherSeason = "Stay another season (free exploration)";
        public const string CertificateHeading = "CERTIFICATE OF FIELD SERVICE";
        public const string CertificateCompletion =
            "completed the AgriVerse field investigation";
        public const string CertificateRecommendation =
            "Recommended intervention";
        public const string CertificateEvidence =
            "Evidence gathered: three water tests, three stakeholder interviews, a five-year simulation, grounded feedback, and a revised policy brief.";

        public static readonly string[] PredictionPrompts =
        {
            "This plot sits nearest the sea. What do you expect - water rice can live with, or water that has already turned against it?",
            "Halfway between river and sea. Safe, or on the edge?",
            "Far from the coast, fed by the river. Your prediction?"
        };

        public static readonly string[][] PredictionChoiceLabels =
        {
            new[] { "Water rice can live with", "Water that has turned against it" },
            new[] { "Safe", "On the edge" },
            new[] { "Fresh enough for rice", "Salt-stressed" }
        };

        public static readonly string[][] PredictionChoiceIds =
        {
            new[] { "rice_can_live", "turned_against_rice" },
            new[] { "safe", "on_the_edge" },
            new[] { "fresh_for_rice", "salt_stressed" }
        };

        public static readonly string[] Glossary =
        {
            "Salinity: how much salt is in the water. Rice starts to suffer around 4 g/L.",
            "g/L: grams of salt in each liter of water. Seawater is about 35; healthy rice water stays under 4.",
            "Brackish: saltier than a river, fresher than the sea.",
            "Salt pattern - persistent brackish: the water stays salty all year.",
            "Salt pattern - brackish dry, fresh wet: salty in the dry season, fresh again when the rains return. Some plans depend on exactly this swing.",
            "Freshwater access: how easily clean water can reach this plot to dilute or flush away salt.",
            "Dry season / wet season: the delta's two worlds. Less river flow in the dry season lets the sea push in; the wet season pushes it back.",
            "Yield (t/ha): how much rice a field produces, in tonnes per hectare. A healthy paddy here is about 6; salt-hit fields can fall to 3 or less.",
            "Income score / sustainability score: 0-100 comparison indexes from the model - higher is better. They compare plans; they are not real-world statistics.",
            "Fit assessment: whether your plan matches this place on four things - the salt level, the seasons, the freshwater, and what the farmer can afford.",
            "Source IDs (S1, S2...): the real published sources behind each fact. Nothing here is invented."
        };

        public static string Arrival(string playerName) =>
            Interpolate(ArrivalTemplate, playerName);

        public static string Ending(string playerName) =>
            Interpolate(EndingTemplate, playerName);

        public static string PredictionPrompt(int siteIndex)
        {
            return siteIndex >= 0 && siteIndex < PredictionPrompts.Length
                ? PredictionPrompts[siteIndex]
                : "What do you expect this sample to show?";
        }

        public static string[] PredictionLabels(int siteIndex)
        {
            return siteIndex >= 0 && siteIndex < PredictionChoiceLabels.Length
                ? PredictionChoiceLabels[siteIndex]
                : new[] { "Lower salinity", "Higher salinity" };
        }

        public static string[] PredictionIds(int siteIndex)
        {
            return siteIndex >= 0 && siteIndex < PredictionChoiceIds.Length
                ? PredictionChoiceIds[siteIndex]
                : new[] { "lower", "higher" };
        }

        private static string Interpolate(string template, string playerName)
        {
            return template.Replace("[PLAYER]", playerName?.Trim() ?? string.Empty);
        }
    }
}
