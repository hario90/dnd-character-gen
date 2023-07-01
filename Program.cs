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


    for (int i = 0; i < 10; i++)
    {
        // ISKFunction[] pipeline = { characterSkill["DnD"], consoleSkill["Listen"] }
        var question = await kernel.RunAsync(context, characterSkill["DnD"]);
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


