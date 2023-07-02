using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.Orchestration;
using Skills;

var kernelSettings = KernelSettings.LoadSettings();

using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
{
    builder
        .SetMinimumLevel(kernelSettings.LogLevel ?? LogLevel.Warning)
        .AddConsole()
        .AddDebug();
});

IKernel kernel = new KernelBuilder()
    .WithLogger(loggerFactory.CreateLogger<IKernel>())
    .WithCompletionService(kernelSettings)
    .Build();

if (kernelSettings.EndpointType == EndpointTypes.TextCompletion)
{
    // note: using skills from the repo
    var skillsDirectory = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "skills");
    var characterSkill = kernel.ImportSemanticSkillFromDirectory(skillsDirectory, "CharacterSkill");
    var consoleSkill = new ConsoleSkill();

    var _consoleSkill = kernel.ImportSkill(consoleSkill);


    var context = new ContextVariables();
    var questionsKey = "questions";
    var answersKey = "answers";
    context.Set(questionsKey, "");
    context.Set(answersKey, "");
    string[] scenarioTypes = {
        "monster encounter: stumbling upon a group of hostile creatures, such as goblins, a dragon, or undead.",
        "puzzle: encountering a complex puzzle or a trap-filled room that requires the answerer to solve riddles, manipulate objects, or make choices to progress or avoid danger.",
        "NPC interaction: meeting non-player characters (NPCs) who provide information, offer quests, trade goods, or present challenges that require diplomatic or persuasive skills to navigate.",
        "Exploration and Discovery:  entering uncharted territories, ancient ruins, or mysterious dungeons, exploring hidden chambers, uncovering forgotten lore, or finding valuable treasures.",
        "Social Encounter: interacting with influential individuals, attending a grand ball, negotiating with political figures, or participating in a trial, relying on social skills, deception, or persuasion to achieve their goals.",
        // "Wilderness Survival:  navigate treacherous terrains, face harsh weather conditions, encounter dangerous wildlife, and must find food, water, and shelter while avoiding environmental hazards.",
        "Moral Dilemma: facing a situation that tests the answerer's ethics and values, requiring them to make difficult choices with far-reaching consequences, such as saving an innocent at the cost of revealing a hidden location.",
        // "Investigation and Mystery",
        // "Dungeon Crawling",
        "Epic Battle or War: engaging in a large-scale conflict, participating in a battle or war between factions, leading troops, and strategizing to achieve victory while facing overwhelming odds."
    };

    for (int i = 0; i < scenarioTypes.Length; i++)
    {
        var scenarioType = scenarioTypes[i];
        var scenarioTypeParts = scenarioType.Split(": ");
        context.Set("scenarioType", scenarioTypeParts[0]);
        context.Set("scenarioTypeDetail", scenarioTypeParts[1]);
        // ISKFunction[] pipeline = { characterSkill["DnD"], consoleSkill["Listen"] }
        var scenario = await kernel.RunAsync(context, characterSkill["DnD"]);
        Console.WriteLine(scenario);
        SKContext? question = await kernel.RunAsync(context, characterSkill["QuestionGenerator"]);
        Console.WriteLine(question);
        string? questions;
        context.TryGetValue(questionsKey, out questions);
        questions += ", " + question;
        context.Set(questionsKey, questions);

        var answer = await kernel.RunAsync(context, _consoleSkill["Listen"]);
        string? answers;
        context.TryGetValue(answersKey, out answers);
        answers += ", " + answer;
        context.Set(answersKey, answers);
    }

    var character = await kernel.RunAsync(context, characterSkill["CharacterGenerator"]);
    Console.WriteLine("Congratulations, you are a: " + character);
}
else if (kernelSettings.EndpointType == EndpointTypes.ChatCompletion)
{
    var chatCompletionService = kernel.GetService<IChatCompletion>();

    var chat = chatCompletionService.CreateNewChat("You are an AI assistant that helps people find information.");
    chat.AddMessage(AuthorRole.User, "Hi, what information can yo provide for me?");

    string response = await chatCompletionService.GenerateMessageAsync(chat, new ChatRequestSettings());
    Console.WriteLine(response);
}


