using OwnExpeditor.Data;
using OwnExpeditor.Sii;

namespace OwnExpeditor.Core.Tests;

/// <summary>
/// Fluxo completo com dados reais (dataset/scs e samples/, fora do git). Sem eles, não faz nada.
/// </summary>
public class EndToEndTests
{
    [Fact]
    public void Real_save_inject_and_write_as_new_save()
    {
        var root = FindRoot();
        var scs = root is null ? null : Path.Combine(root, "dataset", "scs");
        var slot = root is null ? null : Path.Combine(root, "samples", "4A2E204F776E657469", "save", "3");
        if (scs is null || !File.Exists(Path.Combine(scs, "def.scs")) || !Directory.Exists(slot)) return;

        var docs = Directory.CreateTempSubdirectory();
        try
        {
            var saveDir = Path.Combine(docs.FullName, "profiles", "ABC", "save", "3");
            Directory.CreateDirectory(saveDir);
            foreach (var f in new[] { "game.sii", "info.sii" })
                File.Copy(Path.Combine(slot, f), Path.Combine(saveDir, f));

            var catalog = GameCatalog.LoadFromGameDir(scs);
            var profile = Assert.Single(SaveLocator.Profiles(docs.FullName));
            var save = Assert.Single(SaveLocator.Saves(profile));
            var session = new SaveSession(SiiDocument.Load(save.GameSii), catalog);

            var origins = session.Origins();
            Assert.NotEmpty(origins);
            var origin = origins.First(o => session.CargosFor(o).Any(c => session.Destinations(o, c).Count > 0));
            var cargo = session.CargosFor(origin).First(c => session.Destinations(origin, c).Count > 0);
            var target = session.Destinations(origin, cargo)[0];

            var id = session.Inject(origin, target, cargo, 500);
            var folder = SaveLocator.WriteAsNewSave(profile, save, session.Document, "TESTE E2E");

            Assert.Equal("4", Path.GetFileName(folder));
            var written = SiiDocument.Load(Path.Combine(folder, "game.sii"));
            Assert.Equal($"\"{target.Key}\"", written.Find(id)!.Get("target"));
            Assert.Contains(SaveLocator.Saves(profile), s => s.Name == "TESTE E2E");
            Assert.Equal(File.ReadAllBytes(Path.Combine(slot, "game.sii")), File.ReadAllBytes(save.GameSii));
        }
        finally { docs.Delete(recursive: true); }
    }

    private static string? FindRoot()
    {
        for (var d = new DirectoryInfo(AppContext.BaseDirectory); d is not null; d = d.Parent)
            if (Directory.Exists(Path.Combine(d.FullName, "dataset"))) return d.FullName;
        return null;
    }
}
