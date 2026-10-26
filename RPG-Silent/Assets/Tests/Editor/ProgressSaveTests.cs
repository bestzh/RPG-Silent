using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace RPGSilent.Tests
{
    public class ProgressSaveTests
    {
        private string directory;
        private string testRoot;
        private object store;
        private object data;
        private string Primary => GameAssembly.Property<string>(store, "PrimaryPath");
        private string Backup => GameAssembly.Property<string>(store, "BackupPath");

        [SetUp] public void SetUp()
        {
            testRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/AutomatedSaveTests"));
            directory = Path.Combine(testRoot, Guid.NewGuid().ToString("N"));
            store = GameAssembly.New("ProgressSaveStore", directory);
            data = GameAssembly.New("RPGSilent.Domain.PlayerProgressData");
            GameAssembly.Set(data, "profileId", Guid.NewGuid().ToString("N"));
            GameAssembly.Set(data, "gold", 50);
            GameAssembly.Set(data, "totalExp", 125);
            GameAssembly.Set(data, "stance", 0);
            GameAssembly.Set(data, "skillIds", Enumerable.Repeat(string.Empty, 10).ToArray());
        }

        [TearDown] public void TearDown()
        {
            Assert.That(Path.GetDirectoryName(Path.GetFullPath(directory)), Is.EqualTo(testRoot));
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }

        private bool Save(bool newProfile)
        {
            object[] args = { data, newProfile, null };
            return (bool)GameAssembly.Call(store, "TrySave", args);
        }
        private object Load() => GameAssembly.Call(store, "Load");
        private string Status(object result) => GameAssembly.Field<object>(result, "Status").ToString();

        [Test] public void FreshStoreReadsSavedProgressAndIgnoresInterruptedTempFile()
        {
            Assert.That(Status(Load()), Is.EqualTo("Missing"));
            Assert.That(Save(true), Is.True);
            Assert.That(File.Exists(Backup), Is.True);
            File.WriteAllText(Primary + ".tmp", "interrupted");
            var fresh = GameAssembly.New("ProgressSaveStore", directory);
            var restored = GameAssembly.Call(fresh, "Load");
            Assert.That(Status(restored), Is.EqualTo("Loaded"));
            Assert.That(GameAssembly.Field<int>(GameAssembly.Field<object>(restored, "Data"), "totalExp"), Is.EqualTo(125));
        }

        [Test] public void CorruptPrimaryRecoversPreviousCompleteTransaction()
        {
            Assert.That(Save(true), Is.True);
            GameAssembly.Set(data, "totalExp", 250);
            Assert.That(Save(false), Is.True);
            File.WriteAllText(Primary, "{truncated");
            var restored = Load();
            Assert.That(Status(restored), Is.EqualTo("Recovered"));
            Assert.That(GameAssembly.Field<int>(GameAssembly.Field<object>(restored, "Data"), "totalExp"), Is.EqualTo(125));
            string oldBackup = File.ReadAllText(Backup);
            data = GameAssembly.Field<object>(restored, "Data");
            Assert.That(Save(false), Is.True);
            Assert.That(File.ReadAllText(Backup), Is.EqualTo(oldBackup));
        }

        [Test] public void ChangedPayloadFailsChecksum()
        {
            Assert.That(Save(true), Is.True);
            string original = File.ReadAllText(Primary);
            string changed = original.Replace("125", "126");
            Assert.That(changed, Is.Not.EqualTo(original));
            File.WriteAllText(Primary, changed);
            Assert.That(Status(Load()), Is.EqualTo("Recovered"));
        }

        [Test] public void FutureFormatIsNotDowngradedOrOverwritten()
        {
            Assert.That(Save(true), Is.True);
            string future = File.ReadAllText(Primary).Replace("\"version\": 1", "\"version\": 99");
            File.WriteAllText(Primary, future);
            Assert.That(Status(Load()), Is.EqualTo("Unsupported"));
            Assert.That(Save(false), Is.False);
            Assert.That(File.ReadAllText(Primary), Is.EqualTo(future));
        }

        [Test] public void ExplicitNewGameArchivesOldCopiesAndOwnsItsRecoveryBackup()
        {
            Assert.That(Save(true), Is.True);
            string nextProfile = Guid.NewGuid().ToString("N");
            GameAssembly.Set(data, "profileId", nextProfile);
            GameAssembly.Set(data, "gold", 0);
            Assert.That(Save(true), Is.True);
            Assert.That(Directory.GetFiles(Path.Combine(directory, "archives")).Length, Is.EqualTo(2));
            File.WriteAllText(Primary, "broken");
            var restored = Load();
            Assert.That(Status(restored), Is.EqualTo("Recovered"));
            Assert.That(GameAssembly.Field<string>(GameAssembly.Field<object>(restored, "Data"), "profileId"), Is.EqualTo(nextProfile));
        }

        [Test] public void InvalidDataDoesNotTouchLastGoodSave()
        {
            Assert.That(Save(true), Is.True);
            string original = File.ReadAllText(Primary);
            GameAssembly.Set(data, "gold", -1);
            Assert.That(Save(false), Is.False);
            Assert.That(File.ReadAllText(Primary), Is.EqualTo(original));
            GameAssembly.Set(data, "gold", 0);
            GameAssembly.Set(data, "skillIds", new[] { "incomplete" });
            Assert.That(Save(false), Is.False);
            Assert.That(File.ReadAllText(Primary), Is.EqualTo(original));
        }
    }
}
