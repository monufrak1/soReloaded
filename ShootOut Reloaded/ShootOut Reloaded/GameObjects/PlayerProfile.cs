using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Globalization;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Storage;
using System.IO.IsolatedStorage;

namespace GameObjects
{
    class PlayerProfile
    {
        const string GAME_DATA_FILENAME = "ShootOutReloadedData.txt";
        List<int> weaponUnlockScores;

        // Graphics data
        GraphicsDevice graphicsDevice;
        SpriteBatch spriteBatch;
        ContentManager content;
        SpriteFont font;

        Rectangle gamerTagRect;
        Rectangle statsRect;
        Texture2D background;
        Texture2D medalBackground;

        SpriteFont medalFont;
        Texture2D medalIcon;
        SoundEffect medalSFX;

        // Log in data
        bool isLoggedIn;
        IsolatedStorageFile storageFile;
        string saveGameFileName;
        SignedInGamer playerGamerTag;
        Texture2D gamerPicture;

        int gamerTagWidth;
        int gamerTagHeight;
        Vector2 textPosition;
        Rectangle gamerPicRect;

        // Gameplay Statistics
        int totalScore;
        int shotsFired;
        int targetsHit;
        int multishots;
        int longshots;
        int snipershots;
        int bullseyes;
        int headshots;
        int knockdowns;

        int medalsUnlocked;
        Dictionary<string, Medal> medalList;
        Dictionary<string, int> medalRequirements;

        Dictionary<string, LevelRecord> scoreAttackRecords;

        Dictionary<string, LevelRecord> weaponChallengeRecords;
        Dictionary<string, WeaponChallenge> weaponChallenges;

        Dictionary<string, bool> secretsFound;

        string selectedWeaponName;
        Dictionary<string, WeaponRecord> weaponRecords;
        Dictionary<string, GunStats> weaponStats;
        Dictionary<string, bool> unlockedWeapons;

        // Profile Settings
        bool invertCameraY;
        float cameraSensitivityX;
        float cameraSensitivityY;
        bool musicEnabled;
        float musicVolume;

        public PlayerProfile(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, ContentManager content)
        {
            this.graphicsDevice = graphicsDevice;
            this.spriteBatch = spriteBatch;
            this.content = content;

            saveGameFileName = "ShootOutReloaded.dat";

            medalList = new Dictionary<string, Medal>();
            medalRequirements = new Dictionary<string, int>();

            scoreAttackRecords = new Dictionary<string, LevelRecord>();

            weaponUnlockScores = new List<int>();
            weaponChallengeRecords = new Dictionary<string, LevelRecord>();
            weaponChallenges = new Dictionary<string, WeaponChallenge>();

            secretsFound = new Dictionary<string, bool>();

            weaponRecords = new Dictionary<string, WeaponRecord>();
            weaponStats = new Dictionary<string, GunStats>();
            unlockedWeapons = new Dictionary<string, bool>();

            selectedWeaponName = "Colt45";

            font = content.Load<SpriteFont>(@"bigFont");
            medalFont = content.Load<SpriteFont>("smallFont");
            medalIcon = content.Load<Texture2D>("medal_icon");
            medalSFX = content.Load<SoundEffect>(@"SoundEffects\medal_successSFX");

            if (ActivePlayer.FullHDEnabled)
            {
                gamerTagWidth = (int)(graphicsDevice.Viewport.Width * 0.25f);
                gamerTagHeight = (int)(graphicsDevice.Viewport.Height * 0.1f);
            }
            else
            {
                gamerTagWidth = (int)(graphicsDevice.Viewport.Width * 0.33f);
                gamerTagHeight = (int)(graphicsDevice.Viewport.Height * 0.15f);
            }

            int xPos = ActivePlayer.FullHDEnabled ? (int)(graphicsDevice.Viewport.Width * 0.65f)
                : (int)(graphicsDevice.Viewport.Width * 0.6f);

            int yPos = ActivePlayer.FullHDEnabled ? (int)(graphicsDevice.Viewport.Height * 0.15f)
                : (int)(graphicsDevice.Viewport.Height * 0.1f);

            gamerTagRect = new Rectangle(xPos, yPos,
                gamerTagWidth,
                gamerTagHeight);

            statsRect = new Rectangle(xPos,
                yPos + (int)(gamerTagRect.Height * 1.5f),
                gamerTagWidth,
                ActivePlayer.FullHDEnabled ? (int)(graphicsDevice.Viewport.Height * 0.5f)
                    : (int)(graphicsDevice.Viewport.Height * 0.6f));

            // Load background texture
            background = content.Load<Texture2D>(@"menu_background");
            medalBackground = content.Load<Texture2D>(@"menu_background");

            // Default profile settings
            invertCameraY = false;
            cameraSensitivityX = GameStateManagement.OptionsMenuScreen.cameraSensitivitySettings[1];
            cameraSensitivityY = GameStateManagement.OptionsMenuScreen.cameraSensitivitySettings[1];
            musicEnabled = true;
            musicVolume = 0.33f;
            MediaPlayer.Volume = musicVolume;
        }

        private void SelectStorageDevice()
        {
            // Select storage device if needed
            storageFile = IsolatedStorageFile.GetUserStoreForApplication();
        }

        public bool IsXboxLiveEnabled()
        {
            return playerGamerTag.IsSignedInToLive;
        }

        /// <summary>
        /// Logs in player. Loads data from file, or creates one if needed.
        /// </summary>
        /// <param name="playerIndex">Index of gamer to login</param>
        /// <returns></returns>
        public bool Login(PlayerIndex playerIndex)
        {
            // Get the gamer
            playerGamerTag = Gamer.SignedInGamers[playerIndex];

            if (playerGamerTag == null)
            {
                // This player has no valid gamertag
                return false;
            }

            SelectStorageDevice();

            // Load gamertag data
            GamerProfile profile = playerGamerTag.GetProfile();
            gamerPicture = Texture2D.FromStream(graphicsDevice,
                profile.GetGamerPicture());

            int picWidth = (int)(gamerPicture.Width * 1.5f);
            int picHeight = (int)(gamerPicture.Height * 1.5f);
            gamerPicRect = new Rectangle((int)(gamerTagRect.X + gamerTagRect.Width * 0.025f),
                gamerTagRect.Y + (gamerTagRect.Height - picHeight) / 2,
                picWidth,
                picHeight);

            Vector2 textSize = font.MeasureString(playerGamerTag.Gamertag);
            textPosition = new Vector2(gamerPicRect.X + (picWidth * 1.2f),
                gamerPicRect.Y);

            saveGameFileName = playerGamerTag.Gamertag + "_ShootOut.dat";

            // Set default settings from gamertag
            invertCameraY = playerGamerTag.GameDefaults.InvertYAxis;

            // Load data from save file
            LoadDataFromFile();

            isLoggedIn = true;
            return true;
        }

        public void Logout()
        {
            // Save data to file
            SaveDataToFile();

            // Log out player
            isLoggedIn = false;
        }

        public void LoadDataFromFile()
        {
            InitializeProfile();

#if XBOX
            if(!Guide.IsTrialMode)
            {
                if (storageFile != null)
                {
                    if (storageFile.FileExists(saveGameFileName))
                    {
                        IsolatedStorageFileStream file = storageFile.OpenFile(saveGameFileName, FileMode.Open, FileAccess.Read);

                        // Read data from file
                        byte[] buffer = new byte[file.Length];
                        file.Read(buffer, 0, buffer.Length);
                        string data = GetString(buffer);
                        DeserializeData(data);

                        file.Close();
                    }
                }
            }
#else
            StreamReader inFile = new StreamReader(saveGameFileName);

            string dataStr = inFile.ReadToEnd();
            DeserializeData(dataStr);
            
            inFile.Close();
#endif
        }

        public void SaveDataToFile()
        {
            if (!Guide.IsTrialMode)
            {
#if XBOX
                if (storageFile != null)
                {
                    IsolatedStorageFileStream file =
                        storageFile.OpenFile(saveGameFileName, FileMode.Create, FileAccess.Write);

                    // Write data to file
                    string data = SerializeData();
                    byte[] buffer = GetBytes(data);
                    file.Write(buffer, 0, buffer.Length);

                    // Close file
                    file.Close();
                }
#else
                StreamWriter outFile = new StreamWriter(saveGameFileName);

                // Write data to file
                string dataStr = SerializeData();
                outFile.Write(dataStr);

                outFile.Close();
#endif
            }
        }

        public void DeleteSaveFile()
        {
            if (storageFile != null)
            {
                if (storageFile.FileExists(saveGameFileName))
                {
                    storageFile.DeleteFile(saveGameFileName);
                }
            }

            // Reset all stats
            totalScore = 0;
            shotsFired = 0;
            targetsHit = 0;
            multishots = 0;
            longshots = 0;
            snipershots = 0;
            bullseyes = 0;
            headshots = 0;
            knockdowns = 0;

            medalsUnlocked = 0;

            medalList = new Dictionary<string, Medal>();
            medalRequirements = new Dictionary<string, int>();

            scoreAttackRecords = new Dictionary<string, LevelRecord>();

            weaponUnlockScores = new List<int>();
            weaponChallengeRecords = new Dictionary<string, LevelRecord>();
            weaponChallenges = new Dictionary<string, WeaponChallenge>();

            secretsFound = new Dictionary<string, bool>();

            weaponRecords = new Dictionary<string, WeaponRecord>();
            weaponStats = new Dictionary<string, GunStats>();
            unlockedWeapons = new Dictionary<string, bool>();

            selectedWeaponName = "Colt45";

            InitializeProfile();
        }

        private string SerializeData()
        {
            // Turn data to be saved into a string object
            string data = string.Empty;

            // GAME DATA
            data += "TOTAL_SCORE\n" + totalScore.ToString() + "\n";
            data += "SHOTS_FIRED\n" + shotsFired.ToString() + "\n";
            data += "TARGETS_HIT\n" + targetsHit.ToString() + "\n";
            data += "MULTI_SHOTS\n" + multishots.ToString() + "\n";
            data += "LONGSHOTS\n" + longshots.ToString() + "\n";
            data += "SNIPERSHOTS\n" + snipershots.ToString() + "\n";
            data += "BULLSEYES\n" + bullseyes.ToString() + "\n";
            data += "HEADSHOTS\n" + headshots.ToString() + "\n";
            data += "KNOCKDOWNS\n" + knockdowns.ToString() + "\n";
            data += "MEDALS_UNLOCKED\n" + medalsUnlocked.ToString() + "\n";

            // MEDALS
            foreach (string medalTitle in medalList.Keys)
            {
                data += medalTitle + "\n" + (medalList[medalTitle].IsUnlocked ?
                    "UNLOCKED" : "LOCKED");
                data += "\n"; 
            }

            // SCORE ATTACK RECORDS
            foreach (string levelName in scoreAttackRecords.Keys)
            {
                LevelRecord record = scoreAttackRecords[levelName];

                // Build record string
                string recordStr = record.Score.ToString() + "_" +
                                   record.Time.ToString(CultureInfo.InvariantCulture) + "_" +
                                   record.ShotsFired.ToString() + "_" +
                                   record.Accuracy.ToString(CultureInfo.InvariantCulture);

                data += levelName + "\n" + recordStr + "\n";
            }

            // WEAPON CHALLENGE RECORDS
            foreach (string levelName in weaponChallengeRecords.Keys)
            {
                LevelRecord record = weaponChallengeRecords[levelName];

                // Build record string
                string recordStr = record.Score.ToString() + "_" +
                                   record.Time.ToString(CultureInfo.InvariantCulture) + "_" +
                                   record.ShotsFired.ToString() + "_" +
                                   record.Accuracy.ToString(CultureInfo.InvariantCulture);

                data += levelName + "\n" + recordStr + "\n";
            }

            // WEAPON CHALLENGE STATUS
            foreach (string levelName in weaponChallenges.Keys)
            {
                string statusStr;
                if (weaponChallenges[levelName].Status == LevelStatus.Unlocked)
                {
                    statusStr = "UNLOCKED";
                }
                else if (weaponChallenges[levelName].Status == LevelStatus.Completed)
                {
                    statusStr = "COMPLETED";
                }
                else
                {
                    statusStr = "LOCKED";
                }

                data += levelName + "\n" + statusStr + "\n";
            }

            // SECRETS FOUND
            foreach (string levelName in secretsFound.Keys)
            {
                data += levelName + "\n" + (secretsFound[levelName] ? "FOUND" : "NOT_FOUND");
                data += "\n";
            }

            // WEAPON RECORDS
            foreach (string weaponName in weaponRecords.Keys)
            {
                WeaponRecord record = weaponRecords[weaponName];
                string recordStr = record.ShotsFired.ToString() + "_" + record.TargetsHit + "_" +
                                    record.Multishots + "_" + record.Longshots + "_" + record.Snipershots + "_" +
                                    record.Bullseyes + "_" + record.Headshots;
                data += weaponName + "\n" + recordStr + "\n";
            }

            // WEAPON UNLOCKS
            foreach (string weaponName in unlockedWeapons.Keys)
            {
                data += weaponName + "\n" + (unlockedWeapons[weaponName] ? "UNLOCKED" : "LOCKED");
                data += "\n";
            }

            // SELECTED WEAPON
            data += "SELECTED_WEAPON" + "\n" + selectedWeaponName + "\n";

            // SETTINGS
            data += "INVERT_CAMERA_Y" + "\n" + (invertCameraY ? "ENABLED" : "DISABLED") + "\n";
            data += "CAMERA_X_SENSITIVITY" + "\n" + cameraSensitivityX.ToString(CultureInfo.InvariantCulture) + "\n";
            data += "CAMERA_Y_SENSITIVITY" + "\n" + cameraSensitivityY.ToString(CultureInfo.InvariantCulture) + "\n";
            data += "MUSIC_VOLUME" + "\n" + (musicEnabled ? "ENABLED" : "DISABLED") + "\n";

            return data;
        }

        private void DeserializeData(string data)
        {
            char[] separator = { '\n' };
            string[] dataArray = data.Split(separator);
            Dictionary<string, string> dataDictionary = new Dictionary<string, string>();

            for (int i = 0; i < dataArray.Length && dataArray[i] != ""; i+=2)
            {
                // Store key/value pairs into the dictionary
                dataDictionary.Add(dataArray[i], dataArray[i + 1]);
            }

            foreach (string key in dataDictionary.Keys)
            {
                if (key == "TOTAL_SCORE")
                {
                    totalScore = int.Parse(dataDictionary[key]);
                }
                else if (key == "SHOTS_FIRED")
                {
                    shotsFired = int.Parse(dataDictionary[key]);
                }
                else if (key == "TARGETS_HIT")
                {
                    targetsHit = int.Parse(dataDictionary[key]);
                }
                else if (key == "MULTI_SHOTS")
                {
                    multishots = int.Parse(dataDictionary[key]);
                }
                else if (key == "LONGSHOTS")
                {
                    longshots = int.Parse(dataDictionary[key]);
                }
                else if (key == "SNIPERSHOTS")
                {
                    snipershots = int.Parse(dataDictionary[key]);
                }
                else if (key == "BULLSEYES")
                {
                    bullseyes = int.Parse(dataDictionary[key]);
                }
                else if (key == "HEADSHOTS")
                {
                    headshots = int.Parse(dataDictionary[key]);
                }
                else if (key == "KNOCKDOWNS")
                {
                    knockdowns = int.Parse(dataDictionary[key]);
                }
                else if (key == "MEDALS_UNLOCKED")
                {
                    medalsUnlocked = int.Parse(dataDictionary[key]);
                }
                else if (medalList.ContainsKey(key))
                {
                    if (dataDictionary[key] == "UNLOCKED")
                    {
                        // Set this medal to be unlocked
                        medalList[key].IsUnlocked = true;
                    }
                }
                else if (scoreAttackRecords.ContainsKey(key))
                {
                    // Decode level record
                    string recordStr = dataDictionary[key];
                    string[] records = recordStr.Split('_');

                    int score = int.Parse(records[0]);
                    float time = float.Parse(records[1], CultureInfo.InvariantCulture);
                    int shotsFired = int.Parse(records[2]);
                    float accuracy = float.Parse(records[3], CultureInfo.InvariantCulture);

                    LevelRecord levelRecord = new LevelRecord(score, time, shotsFired, accuracy);

                    if (!levelRecord.Equals(LevelRecord.ZeroRecord))
                    {
                        // Update the record
                        scoreAttackRecords[key] =
                            scoreAttackRecords[key].UpdateRecord(levelRecord);
                    }
                }
                else if (weaponChallengeRecords.ContainsKey(key))
                {
                    // Decode level record
                    string recordStr = dataDictionary[key];
                    string[] records = recordStr.Split('_');

                    int score = int.Parse(records[0]);
                    float time = float.Parse(records[1], CultureInfo.InvariantCulture);
                    int shotsFired = int.Parse(records[2]);
                    float accuracy = float.Parse(records[3], CultureInfo.InvariantCulture);

                    LevelRecord levelRecord = new LevelRecord(score, time, shotsFired, accuracy);

                    if (!levelRecord.Equals(LevelRecord.ZeroRecord))
                    {
                        // Update the record
                        weaponChallengeRecords[key] =
                            weaponChallengeRecords[key].UpdateRecord(levelRecord);
                    }
                }
                else if (weaponChallenges.ContainsKey(key))
                {
                    string statusStr = dataDictionary[key];
                    LevelStatus status;

                    if (statusStr == "UNLOCKED")
                    {
                        status = LevelStatus.Unlocked;
                    }
                    else if (statusStr == "COMPLETED")
                    {
                        status = LevelStatus.Completed;
                    }
                    else
                    {
                        status = LevelStatus.Locked;
                    }

                    weaponChallenges[key].Status = status;
                }
                else if (secretsFound.ContainsKey(key))
                {
                    secretsFound[key] = (dataDictionary[key] == "FOUND");
                }
                else if (weaponRecords.ContainsKey(key))
                {
                    string[] records = dataDictionary[key].Split('_');
                    int shotsFired = int.Parse(records[0]);
                    int targetsHit = int.Parse(records[1]);
                    int multishots = int.Parse(records[2]);
                    int longshots = int.Parse(records[3]);
                    int snipershots = int.Parse(records[4]);
                    int bullseyes = int.Parse(records[5]);
                    int headshots = int.Parse(records[6]);
                    WeaponRecord record = new WeaponRecord(shotsFired, targetsHit, multishots, longshots, snipershots,
                                                            bullseyes, headshots);
                    weaponRecords[key] = record;
                }
                else if (unlockedWeapons.ContainsKey(key))
                {
                    unlockedWeapons[key] = (dataDictionary[key] == "UNLOCKED");
                }
                else if (key == "SELECTED_WEAPON")
                {
                    selectedWeaponName = dataDictionary[key];
                }
                else if (key == "INVERT_CAMERA_Y")
                {
                    invertCameraY = (dataDictionary[key] == "ENABLED");
                }
                else if (key == "CAMERA_X_SENSITIVITY")
                {
                    cameraSensitivityX = float.Parse(dataDictionary[key], CultureInfo.InvariantCulture);
                }
                else if (key == "CAMERA_Y_SENSITIVITY")
                {
                    cameraSensitivityY = float.Parse(dataDictionary[key], CultureInfo.InvariantCulture);
                }
                else if (key == "MUSIC_VOLUME")
                {
                    MusicEnabled = (dataDictionary[key] == "ENABLED");
                }
            }
        }

        private byte[] GetBytes(string str)
        {
            byte[] bytes = new byte[str.Length * sizeof(char)];
            System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

        private string GetString(byte[] bytes)
        {
            char[] chars = new char[bytes.Length / sizeof(char)];
            System.Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
            return new string(chars);
        }

        private void InitializeProfile()
        {
            List<Medal> gunMedals = new List<Medal>();
            List<Medal> secretMedals = new List<Medal>();
            List<Medal> scoreAttackMedals = new List<Medal>();

            // Read in game data from file
            StreamReader inFile = new StreamReader(GAME_DATA_FILENAME);
            string line;

            // TESTING 
            // Add all Levels in ScoreAttack directory
            DirectoryInfo di = new DirectoryInfo(@"Levels\ScoreAttack");
            FileInfo[] files = di.GetFiles();
            foreach (FileInfo file in files)
            {
                string levelFileName = file.Name;

                // Create level record
                scoreAttackRecords.Add(levelFileName,
                    LevelRecord.ZeroRecord);

                secretsFound.Add(levelFileName + "_SECRET", false);
            }
            // TESTING

            while (!inFile.EndOfStream)
            {
                line = inFile.ReadLine();

                if(line == "SCORE_ATTACK_LEVEL")
                {
                    //string levelFileName = inFile.ReadLine();

                    //// Create level record
                    //scoreAttackRecords.Add(levelFileName,
                    //    LevelRecord.ZeroRecord);
                    //scoreAttackMedals.Add(new Medal(graphicsDevice, spriteBatch, font, medalFont,
                    //                medalBackground, medalIcon, medalSFX,
                    //                levelFileName.Split('.')[0] + " Complete",
                    //                "Complete the " + levelFileName.Split('.')[0] + "\n" +
                    //                "Score Attack level"));

                    //secretsFound.Add(levelFileName + "_SECRET", false);
                    //secretMedals.Add(new Medal(graphicsDevice, spriteBatch, font, medalFont,
                    //                medalBackground, medalIcon, medalSFX,
                    //                levelFileName.Split('.')[0] + " Explorer",
                    //                "Find the hidden Grenade" + "\n" +
                    //                "in the " + levelFileName.Split('.')[0] + " level"));

                }
                else if (line == "WEAPON_CHALLENGE")
                {
                    string levelFileName = inFile.ReadLine();
                    string challengeInfo = inFile.ReadLine();

                    // Create new file record
                    weaponChallengeRecords.Add(levelFileName,
                        LevelRecord.ZeroRecord);

                    LevelStatus status = weaponChallenges.Count == 0 ? LevelStatus.Unlocked : LevelStatus.Locked;
                    ShooterGameType gameType = ShooterGameType.Targets;
                    float targetScoreOrTime = 0.0f;

                    if (challengeInfo == "TARGETS")
                    {
                        gameType = ShooterGameType.Targets;
                    }
                    else if (challengeInfo == "COLLECTION")
                    {
                        gameType = ShooterGameType.Collection;
                    }
                    else if (challengeInfo == "BULLSEYE_CHALLENGE")
                    {
                        gameType = ShooterGameType.BullseyeChallenge;
                    }
                    else if (challengeInfo == "HEADSHOT_CHALLENGE")
                    {
                        gameType = ShooterGameType.HeadshotChallenge;
                    }
                    else
                    {
                        // Get more data from the line
                        string type = challengeInfo.Split(' ')[0];
                        targetScoreOrTime = float.Parse(challengeInfo.Split(' ')[1], CultureInfo.InvariantCulture);

                        if (type == "HIGH_SCORE")
                        {
                            gameType = ShooterGameType.TargetScore;
                        }
                        else if (type == "TIME_TRIAL")
                        {
                            gameType = ShooterGameType.TimeTrial;
                        }
                    }

                    WeaponChallenge challengeData = new WeaponChallenge(status, gameType, targetScoreOrTime);
                    weaponChallenges.Add(levelFileName + "_STATUS", challengeData);
                }
                else if (line == "WEAPON")
                {
                    string weaponFileName = inFile.ReadLine();
                    int requirement = int.Parse(inFile.ReadLine());

                    // Create weapon entry
                    weaponRecords.Add(weaponFileName, new WeaponRecord(0, 0, 0, 0, 0, 0, 0));
                    weaponStats.Add(weaponFileName, new GunStats(weaponFileName));
                    unlockedWeapons.Add(weaponFileName + "_UNLOCKED", unlockedWeapons.Count == 0 ? true : false);
                    weaponUnlockScores.Add(requirement);

                    if (unlockedWeapons.Count > 1)
                    {
                        // Create medals for weapon unlocks
                        gunMedals.Add(new Medal(graphicsDevice, spriteBatch, font, medalFont,
                                    medalBackground, medalIcon, medalSFX,
                                    weaponFileName.Split('.')[0],
                                    "Unlock the " + weaponFileName.Split('.')[0] + "\n" +
                                        "Weapon and Challenge"));
                    }
                }
            }

            string title;

            // Create medals
            title = "Target Noob";
            medalRequirements.Add(title, 10);
            AddMedal(title, "Shoot " + medalRequirements[title] + " targets");

            title = "Target Champ";
            medalRequirements.Add(title, 100);
            AddMedal(title, "Shoot " + medalRequirements[title] + " targets");

            title = "Target Pro";
            medalRequirements.Add(title, 250);
            AddMedal(title, "Shoot " + medalRequirements[title] + " targets");

            title = "Target Master";
            medalRequirements.Add(title, 800);
            AddMedal(title, "Shoot " + medalRequirements[title] + " targets");

            title = "Bullet Noob";
            medalRequirements.Add(title, 100);
            AddMedal(title, "Fire " + medalRequirements[title] + " shots");

            title = "Bullet Champ";
            medalRequirements.Add(title, 500);
            AddMedal(title, "Fire " + medalRequirements[title] + " shots");

            title = "Bullet Pro";
            medalRequirements.Add(title, 1500);
            AddMedal(title, "Fire " + medalRequirements[title] + " shots");

            title = "Bullet Master";
            medalRequirements.Add(title, 3000);
            AddMedal(title, "Fire " + medalRequirements[title] + " shots");

            title = "ShootOut Noob";
            medalRequirements.Add(title, 10000);
            AddMedal(title, "Get a Total Score" + "\n" +
                            "of " + medalRequirements[title]);

            title = "ShootOut Champ";
            medalRequirements.Add(title, 65000);
            AddMedal(title, "Get a Total Score" + "\n" +
                            "of " + medalRequirements[title]);

            title = "ShootOut Pro";
            medalRequirements.Add(title, 150000);
            AddMedal(title, "Get a Total Score" + "\n" +
                            "of " + medalRequirements[title]);

            title = "ShootOut Master";
            medalRequirements.Add(title, 250000);
            AddMedal(title, "Get a Total Score" + "\n" +
                            "of " + medalRequirements[title]);

            title = "Double Down";
            medalRequirements.Add(title, 50);
            AddMedal(title, "Get " + medalRequirements[title] + " MULTI-SHOTS");

            title = "Marksman";
            medalRequirements.Add(title, 40);
            AddMedal(title, "Get " + medalRequirements[title] + " LONGSHOTS");

            title = "Sniper";
            medalRequirements.Add(title, 10);
            AddMedal(title, "Get " + medalRequirements[title] + " SNIPERSHOTS");

            title = "BULLSEYE!";
            medalRequirements.Add(title, 200);
            AddMedal(title, "Get " + medalRequirements[title] + " BULLSEYES");

            title = "Head Hunter";
            medalRequirements.Add(title, 100);
            AddMedal(title, "Get " + medalRequirements[title] + " HEADSHOTS");

            title = "PunchOut";
            medalRequirements.Add(title, 25);
            AddMedal(title, "Get " + medalRequirements[title] + " KNOCKDOWNS");

            title = "Consolation Prize";
            medalRequirements.Add(title, 500);
            AddMedal(title, "Get " + medalRequirements[title] + " misses...");

            foreach (Medal m in scoreAttackMedals)
            {
                medalList.Add(m.Title, m);
            }

            AddMedal("Score Attack Master",
                "Complete all Score Attack levels");

            AddMedal("A+ Student",
                "Get a Bullseye, a Headshot," + "\n" +
                "a Multi-Shot, a Longshot," + "\n" +
                "and a Snipershot on Training");

            title = "Lucky #7";
            medalRequirements.Add(title, 7);
            AddMedal(title, "Get at least " + medalRequirements[title] + " Multi-Shots" + "\n" +
                "in the Rust level");

            AddMedal("Compound Cleared",
                "Complete the Compound level" + "\n" +
                "with only Headshots");

            AddMedal("Two for One",
                "Get 2 Headshots in one" + "\n" +
                "shot on the Stacks 2 level");

            title = "Target Flood";
            medalRequirements.Add(title, 8);
            AddMedal(title,
                "Shoot " + medalRequirements[title] + " targets in" + "\n" +
                "rapid succession on the" + "\n" +
                "Flooded level");

            title = "Drill, Baby, Drill";
            medalRequirements.Add(title, 15);
            AddMedal(title,
                "Get at least " + medalRequirements[title] + " Longshots" + "\n" +
                "on the Pipeline level");

            title = "Speedy Miner";
            medalRequirements.Add(title, 20);
            AddMedal(title,
                "Complete the Mine" + "\n" +
                "level in " + medalRequirements[title] + " secs");
                
            AddMedal("Perfect Storage",
                "Complete the Storage level" + "\n" +
                "with only Bullseyes");

            title = "Bog Boxer";
            medalRequirements.Add(title, 10);
            AddMedal(title,
                "Complete the Bog level with" + "\n" +
                "at least " + medalRequirements[title] + " knockdowns");

            title = "Prison Riot";
            medalRequirements.Add(title, 4);
            AddMedal(title, "Get a Multi-Shot of " + medalRequirements[title] + "\n" +
                            "in the Prison level");

            title = "High Roller";
            medalRequirements.Add(title, 10000);
            AddMedal(title, "Complete all Score Attack" + "\n" +
                            "levels with a High Score" + "\n" +
                            "over " + medalRequirements[title]);

            title = "1/2 Minute Man";
            medalRequirements.Add(title, 45);
            AddMedal(title, "Complete all Score Attack" + "\n" +
                            "levels in under " + medalRequirements[title] + " secs");

            title = "Minimalist";
            medalRequirements.Add(title, 20);
            AddMedal(title, "Complete all Score Attack" + "\n" +
                            "levels in under " + medalRequirements[title] + " shots");

            title = "True Shot";
            AddMedal(title, "Complete all Score Attack" + "\n" +
                            "levels with 100% accuracy");

            AddMedal("WTF?", "Complete a level with an" + "\n" +
                       "accuracy over 100%");

            foreach (Medal m in gunMedals)
            {
                medalList.Add(m.Title, m);
            }

            AddMedal("One Man Army",
                "Unlock all weapons");

            AddMedal("Colt45 Expert", "Complete the Colt45 Challenge");

            AddMedal("SCARL Expert", "Complete the SCARL Challenge");

            AddMedal("Thompson Expert", "Complete the Thompson Challenge");

            AddMedal("M4A1 Expert", "Complete the M4A1 Challenge");

            AddMedal("PP-91 Expert", "Complete the PP-91 Challenge");

            AddMedal("Kriss Expert", "Complete the Kriss Challenge");

            AddMedal("Weapons Master", "Complete all\n" +
                                       "Weapon Challenges");

            AddMedal("Challenge Master",
                "Complete all Weapon" + "\n" +
                "Challenges with 100%" + "\n" +
                "Accuracy");

            title = "Colt45 Specialist";
            medalRequirements.Add(title, 35);
            AddMedal(title,
                "Complete the Colt45 Challenge" + "\n" +
                "in under " + medalRequirements[title] + " secs");

            title = "SCARL Specialist";
            medalRequirements.Add(title, 10000);
            AddMedal(title,
                "Complete the SCARL Challenge" + "\n" +
                "with a Score over " + medalRequirements[title]);

            AddMedal("Thompson Specialist",
                "Complete the Thompson Challenge" + "\n" +
                "getting only Bullseyes");

            title = "M4A1 Specialist";
            medalRequirements.Add(title, 60);
            AddMedal(title,
                "Complete the M4A1 Challenge" + "\n" +
                "in under " + medalRequirements[title] + " secs");

            title = "PP-91 Specialist";
            medalRequirements.Add(title, 90);
            AddMedal(title,
                "Complete the PP-91 Challenge" + "\n" +
                "in under " + medalRequirements[title] + " seconds");

            AddMedal("Kriss Specialist",
                "Complete the Kriss Challenge" + "\n" +
                "with only Knockdowns");

            title = "Pistol Pete";
            medalRequirements.Add(title, 3);
            AddMedal(title,
                "Get " + medalRequirements[title] + " Headshots in rapid" + "\n" +
                "succession with the Colt45.");

            title = "Rifleman";
            medalRequirements.Add(title, 2);
            AddMedal(title, "Get " + medalRequirements[title] + " Longshots in rapid\n" +
                "succession with the SCARL");

            title = "Chicago Typewriter";
            medalRequirements.Add(title, 5);
            AddMedal(title,
                "Get " + medalRequirements[title] + " Headshots in a" + "\n" +
                "row using the Thompson");

            title = "This Is My Rifle";
            medalRequirements.Add(title, 10);
            AddMedal(title,
                "Get " + medalRequirements[title] + " Bullseyes in a" + "\n" +
                "row using the M4A1");

            AddMedal("PP-91 Perfectionist",
                "Complete any level using the" + "\n" +
                "PP-91 with 100% Accuracy, and" + "\n" +
                "get a Bullseye and a Headshot");

            title = "Krisstastic";
            medalRequirements.Add(title, 5);
            AddMedal(title,
                "Get " + medalRequirements[title] + " Bullseyes in rapid" + "\n" +
                "succession with the Kriss");

            AddMedal("X-Ray Vision",
                "Shoot a target\n" +
                "through an object");

            title = "Jack of all Trades";
            medalRequirements.Add(title, 100);
            AddMedal(title, "Shoot " + medalRequirements[title] + " targets\n" +
                            "with every weapon");

            title = "Weapons Tester";
            medalRequirements.Add(title, 100);
            AddMedal(title, "Fire every weapon" + "\n" +
                            medalRequirements[title] + " times");

            // Add individual secret item medals
            foreach (Medal m in secretMedals)
            {
                medalList.Add(m.Title, m);
            }

            AddMedal("Master Collector",
                "Find all Grenades");

            AddMedal("Nice to meet you", "???");

            AddMedal("UnBound", 
                "Available now on" + "\n" +
                "Xbox Live Indie Games");

            AddMedal("ShootOut",
                "Available now on" + "\n" +
                "Xbox Live Indie Games");

            AddMedal("Master Orchestrator", "D-Mav on the Mic!");

            AddMedal("Master Medal", 
                "Unlock every Medal in" + "\n" +
                "ShootOut Reloaded");
        }

        public bool UpdateLevelRecord(string levelName, LevelRecord newRecord)
        {
            // Search for level record to update
            bool isNewRecord = false;
            if (scoreAttackRecords.ContainsKey(levelName))
            {
                LevelRecord oldRecord = null;
                oldRecord = scoreAttackRecords[levelName];
                scoreAttackRecords[levelName] =
                    scoreAttackRecords[levelName].UpdateRecord(newRecord);

                isNewRecord = !(scoreAttackRecords[levelName].Equals(oldRecord));
                totalScore += newRecord.Score;

                // Unlock Level Medal
                UnlockMedal(levelName.Split('.')[0] + " Complete");
            }
            else if (weaponChallengeRecords.ContainsKey(levelName))
            {
                LevelRecord oldRecord = null;
                oldRecord = weaponChallengeRecords[levelName];
                weaponChallengeRecords[levelName] =
                    weaponChallengeRecords[levelName].UpdateRecord(newRecord);

                isNewRecord = !(weaponChallengeRecords[levelName].Equals(oldRecord));

                if (newRecord.Score > oldRecord.Score)
                {
                    // Add the extra amount to the total score
                    totalScore += newRecord.Score - oldRecord.Score;
                }
            }

            // Check for weapon unlocks
            int unlockedWeaponsCount = 0;
            for (int i = 0; i < weaponUnlockScores.Count; i++)
            {
                if (totalScore >= weaponUnlockScores[i])
                {
                    unlockedWeaponsCount++;

                    // Unlock weapon and challenge
                    if (!unlockedWeapons[unlockedWeapons.Keys.ElementAt(i)])
                    {
                        unlockedWeapons[unlockedWeapons.Keys.ElementAt(i)] = true;
                        weaponChallenges[weaponChallenges.Keys.ElementAt(i)].Status = LevelStatus.Unlocked;

                        UnlockMedal(unlockedWeapons.Keys.ElementAt(i).Split('.')[0]);
                    }
                }
            }

            if (unlockedWeaponsCount == unlockedWeapons.Count)
            {
                UnlockMedal("One Man Army");
            }

            // Medal Checks
            bool completedAll = true;
            bool minuteManCheck = true;
            bool highRollerCheck = true;
            bool minimalistCheck = true;
            bool trueShotCheck = true;
            foreach(LevelRecord record in scoreAttackRecords.Values)
            {
                if (record != LevelRecord.ZeroRecord)
                {
                    if (record.Time > medalRequirements["1/2 Minute Man"])
                    {
                        minuteManCheck = false;
                    }

                    if (record.Score < medalRequirements["High Roller"])
                    {
                        highRollerCheck = false;
                    }

                    if (record.ShotsFired > medalRequirements["Minimalist"])
                    {
                        minimalistCheck = false;
                    }

                    if (record.Accuracy < 1.0f)
                    {
                        trueShotCheck = false;
                    }
                }
                else
                {
                    completedAll = false;
                    minuteManCheck = false;
                    highRollerCheck = false;
                    minimalistCheck = false;
                    trueShotCheck = false;
                }
            }

            if (completedAll)
            {
                UnlockMedal("Score Attack Master");
            }

            if (minuteManCheck)
            {
                UnlockMedal("1/2 Minute Man");
            }

            if (highRollerCheck)
            {
                UnlockMedal("High Roller");
            }

            if (minimalistCheck)
            {
                UnlockMedal("Minimalist");
            }

            if (trueShotCheck)
            {
                UnlockMedal("True Shot");
            }

            completedAll = true;
            bool challengeMaster = true;
            foreach (string levelKey in weaponChallengeRecords.Keys)
            {
                if (weaponChallengeRecords[levelKey] != LevelRecord.ZeroRecord)
                {
                    // This challenge has been completed
                    weaponChallenges[levelKey + "_STATUS"].Status = LevelStatus.Completed;
                    UnlockMedal(levelKey.Split(' ')[0] + " Expert");

                    if (weaponChallengeRecords[levelKey].Accuracy < 1.0f)
                    {
                        challengeMaster = false;
                    }
                }
                else
                {
                    completedAll = false;
                    challengeMaster = false;
                }
            }

            if (completedAll)
            {
                UnlockMedal("Weapons Master");
            }

            if (challengeMaster)
            {
                UnlockMedal("Challenge Master");
            }

            // Unlock medals
            if (totalScore >= medalRequirements["ShootOut Noob"])
            {
                UnlockMedal("ShootOut Noob");
            }

            if (totalScore >= medalRequirements["ShootOut Champ"])
            {
                UnlockMedal("ShootOut Champ");
            }

            if (totalScore >= medalRequirements["ShootOut Pro"])
            {
                UnlockMedal("ShootOut Pro");
            }

            if (totalScore >= medalRequirements["ShootOut Master"])
            {
                UnlockMedal("ShootOut Master");
            }

            if (newRecord.Accuracy > 1.0f)
            {
                UnlockMedal("WTF?");
            }

            if (levelName == "Colt45 Challenge.lvl" &&
                newRecord.Time <= medalRequirements["Colt45 Specialist"])
            {
                UnlockMedal("Colt45 Specialist");
            }

            if (levelName == "SCARL Challenge.lvl" &&
                newRecord.Score >= medalRequirements["SCARL Specialist"])
            {
                UnlockMedal("SCARL Specialist");
            }

            if (levelName == "Thompson Challenge.lvl" &&
                newRecord.NumBullseyes == newRecord.NumTargetsShot + newRecord.NumKnockdowns)
            {
                UnlockMedal("Thompson Specialist");
            }

            if (levelName == "M4A1 Challenge.lvl" &&
                newRecord.Time <= medalRequirements["M4A1 Specialist"])
            {
                UnlockMedal("M4A1 Specialist");
            }

            if (newRecord.Time < medalRequirements["PP-91 Specialist"] &&
                levelName == "PP-91 Challenge.lvl")
            {
                UnlockMedal("PP-91 Specialist");
            }

            if (selectedWeaponName == "PP-91" && 
                newRecord.Accuracy >= 1.0f && newRecord.NumBullseyes > 0 &&
                newRecord.NumHeadshots > 0)
            {
                UnlockMedal("PP-91 Perfectionist");
            }
            
            if (levelName == "Kriss Challenge.lvl"
                && newRecord.NumTargetsShot == 0)
            {
                UnlockMedal("Kriss Specialist");
            }

            if (levelName == "Training.lvl" &&
                newRecord.NumBullseyes > 0 && newRecord.NumHeadshots > 0 &&
                newRecord.NumMultishots > 0 && newRecord.NumLongshots > 0 &&
                newRecord.NumSnipershots > 0)
            {
                UnlockMedal("A+ Student");
            }

            if (levelName == "Rust.lvl" &&
                newRecord.NumMultishots >= medalRequirements["Lucky #7"])
            {
                UnlockMedal("Lucky #7");
            }

            if (levelName == "Compound.lvl" &&
                newRecord.NumHeadshots == newRecord.NumTargetsShot + newRecord.NumKnockdowns)
            {
                UnlockMedal("Compound Cleared");
            }

            if (levelName == "Pipeline.lvl" &&
                newRecord.NumLongshots >= medalRequirements["Drill, Baby, Drill"])
            {
                UnlockMedal("Drill, Baby, Drill");
            }

            if (newRecord.Time <= medalRequirements["Speedy Miner"] &&
                levelName == "Mine.lvl")
            {
                UnlockMedal("Speedy Miner");
            }

            if (levelName == "Storage.lvl" &&
               newRecord.NumBullseyes == newRecord.NumTargetsShot + newRecord.NumKnockdowns)
            {
                UnlockMedal("Perfect Storage");
            }

            if (newRecord.NumKnockdowns >= medalRequirements["Bog Boxer"] &&
                levelName == "Bog.lvl")
            {
                UnlockMedal("Bog Boxer");
            }

            SaveDataToFile();

            return isNewRecord;
        }

        public LevelRecord GetLevelRecord(string levelName)
        {
            if (scoreAttackRecords.ContainsKey(levelName))
                return scoreAttackRecords[levelName];
            else if (weaponChallengeRecords.ContainsKey(levelName))
                return weaponChallengeRecords[levelName];

            return LevelRecord.ZeroRecord;
        }

        public bool IsWeaponUnlocked(string weaponName)
        {
            return unlockedWeapons[weaponName + "_UNLOCKED"];
        }

        public void UpdateStatsAndMedals()
        {
            // Check for stats related medals to unlock
            foreach (string medalTitle in medalList.Keys)
            {
                if (medalTitle == "Bullet Noob")
                {
                    if (shotsFired >= medalRequirements[medalTitle])
                    {
                        UnlockMedal(medalTitle);
                    }
                }
                else if (medalTitle == "Bullet Champ")
                {
                    if (shotsFired >= medalRequirements[medalTitle])
                    {
                        UnlockMedal(medalTitle);
                    }
                }
                else if (medalTitle == "Bullet Pro")
                {
                    if (shotsFired >= medalRequirements[medalTitle])
                    {
                        UnlockMedal(medalTitle);
                    }
                }
                else if (medalTitle == "Bullet Master")
                {
                    if (shotsFired >= medalRequirements[medalTitle])
                    {
                        UnlockMedal(medalTitle);
                    }
                }
                else if (medalTitle == "Target Noob")
                {
                    if (targetsHit >= medalRequirements[medalTitle])
                    {
                        UnlockMedal(medalTitle);
                    }
                }
                else if (medalTitle == "Target Champ")
                {
                    if (targetsHit >= medalRequirements[medalTitle])
                    {
                        UnlockMedal(medalTitle);
                    }
                }
                else if (medalTitle == "Target Pro")
                {
                    if (targetsHit >= medalRequirements[medalTitle])
                    {
                        UnlockMedal(medalTitle);
                    }
                }
                else if (medalTitle == "Target Master")
                {
                    if (targetsHit >= medalRequirements[medalTitle])
                    {
                        UnlockMedal(medalTitle);
                    }
                }
                else if (medalTitle == "PunchOut")
                {
                    if (knockdowns >= medalRequirements[medalTitle])
                    {
                        UnlockMedal(medalTitle);
                    }
                }
                else if (medalTitle == "Double Down")
                {
                    if (multishots >= medalRequirements[medalTitle])
                    {
                        UnlockMedal(medalTitle);
                    }
                }
                else if (medalTitle == "Marksman")
                {
                    if (longshots >= medalRequirements[medalTitle])
                    {
                        UnlockMedal(medalTitle);
                    }
                }
                else if (medalTitle == "Sniper")
                {
                    if (snipershots >= medalRequirements[medalTitle])
                    {
                        UnlockMedal(medalTitle);
                    }
                }
                else if (medalTitle == "BULLSEYE!")
                {
                    if (bullseyes >= medalRequirements[medalTitle])
                    {
                        UnlockMedal(medalTitle);
                    }
                }
                else if (medalTitle == "Head Hunter")
                {
                    if (headshots >= medalRequirements[medalTitle])
                    {
                        UnlockMedal(medalTitle);
                    }
                }
                else if (medalTitle == "Consolation Prize")
                {
                    if (Misses >= medalRequirements[medalTitle])
                    {
                        UnlockMedal(medalTitle);
                    }
                }
                else if (medalTitle == "Jack of all Trades")
                {
                    bool unlockMedal = true;
                    foreach (WeaponRecord record in weaponRecords.Values)
                    {
                        if (record.TargetsHit < medalRequirements[medalTitle])
                        {
                            unlockMedal = false;
                        }
                    }

                    if (unlockMedal)
                    {
                        UnlockMedal(medalTitle);
                    }
                }
                else if (medalTitle == "Weapons Tester")
                {
                    bool unlockMedal = true;
                    foreach (WeaponRecord record in weaponRecords.Values)
                    {
                        if (record.ShotsFired < medalRequirements[medalTitle])
                        {
                            unlockMedal = false;
                        }
                    }

                    if (unlockMedal)
                    {
                        UnlockMedal(medalTitle);
                    }
                }
            }
        }

        public void UnlockMedal(string medalTitle)
        {
            // Unlock the medal if it exists
            if (medalList[medalTitle] != null)
            {
                // Unlock the medal
                if (medalList[medalTitle].Unlock()) medalsUnlocked++;
            }

            // Unlock "Master Medal"
            if (medalsUnlocked == medalList.Count - 1)
            {
                UnlockMedal("Master Medal");
            }
        }

        private void AddMedal(string medalTitle, string medalDescription)
        {
            Medal newMedal = new Medal(graphicsDevice, spriteBatch, font, medalFont,
                medalBackground, medalIcon, medalSFX,
                medalTitle, medalDescription);
            medalList.Add(newMedal.Title, newMedal);
        }

        public void SetSecretFound(string levelName)
        {
            string fullName = levelName + "_SECRET";
            if (!secretsFound[fullName])
            {
                // Set data
                secretsFound[fullName] = true;

                UnlockMedal(levelName.Split('.')[0] + " Explorer");

                if (!secretsFound.Values.Contains(false))
                {
                    UnlockMedal("Master Collector");
                }
            }
        }

        public bool IsSecretFound(string levelName)
        {
            return secretsFound[levelName + "_SECRET"];
        }

        public GunStats GetWeaponStats(string weaponName)
        {
            return weaponStats[weaponName];
        }

        public int GetWeaponScoreRequirement(string weaponName)
        {
            return weaponUnlockScores[weaponRecords.Keys.ToList().IndexOf(weaponName)];
        }

        public WeaponChallenge GetWeaponChallenge(string levelName)
        {
            string key = levelName + "_STATUS";
            return weaponChallenges[key];
        }

        public int GetMedalRequirement(string medalTitle)
        {
            return medalRequirements[medalTitle];
        }

        public void Draw(float transitionAlpha)
        {
            DrawGamerTag(transitionAlpha);

            // Draw gamertag
            spriteBatch.Begin();

            // Draw stats
            string statsString = "Total Score  : " + totalScore.ToString() + "\n\n" +
                                    "Targets Shot : " + targetsHit.ToString() + "\n" +
                                    "Shots Fired  : " + shotsFired.ToString() + "\n" +
                                    "Misses       : " + Misses.ToString() + "\n" +
                                    "Accuracy     : " + Accuracy.ToString("P") + "\n\n" +
                                    "Multi-Shots  : " + multishots.ToString() + "\n" +
                                    "Long Shots   : " + longshots.ToString() + "\n" +
                                    "Sniper Shots : " + snipershots.ToString() + "\n" +
                                    "Bullseyes    : " + bullseyes.ToString() + "\n" +
                                    "Headshots    : " + headshots.ToString() + "\n" +
                                    "Knockdowns   : " + knockdowns.ToString();

            float statsScale = ActivePlayer.FullHDEnabled ? 1.0f : 0.85f;
            Vector2 statsSize = font.MeasureString(statsString) * statsScale;
            Vector2 statsPosition =
                new Vector2(statsRect.X + statsRect.Width / 2 - statsSize.X / 2,
                            statsRect.Y + statsRect.Height/2 - statsSize.Y/2);

            spriteBatch.Draw(background, statsRect, Color.White * (transitionAlpha - 0.2f));
            spriteBatch.DrawString(font, statsString, statsPosition,
                    Color.White * transitionAlpha, 0.0f, Vector2.Zero, statsScale, SpriteEffects.None, 0.0f);
  
            spriteBatch.End();
        }

        public void DrawGamerTag(float transitionAlpha)
        {
            if (isLoggedIn)
            {
                spriteBatch.Begin();

                // Draw Gamertag
                spriteBatch.Draw(background, gamerTagRect, Color.White * (transitionAlpha - 0.2f));
                spriteBatch.Draw(gamerPicture, gamerPicRect, Color.White * transitionAlpha);
                spriteBatch.DrawString(font, playerGamerTag.Gamertag,
                    textPosition, Color.White * transitionAlpha);

                string medalsInfo = "Medals: " + medalsUnlocked + "/" + medalList.Count;
                Vector2 medalsInfoSize = font.MeasureString(medalsInfo);
                Vector2 medalsInfoPosition = new Vector2(textPosition.X, textPosition.Y + gamerPicRect.Height - (medalsInfoSize.Y));
                spriteBatch.DrawString(font, medalsInfo,
                    medalsInfoPosition, Color.White * transitionAlpha);

                string weaponInfo = "Weapon: " + selectedWeaponName;
                Vector2 weaponInfoSize = font.MeasureString(weaponInfo);
                Vector2 weaponInfoPosition = new Vector2(textPosition.X, (textPosition.Y + medalsInfoPosition.Y)/2);
                spriteBatch.DrawString(font, weaponInfo, weaponInfoPosition, Color.White * transitionAlpha);

                spriteBatch.End();
            }
        }

        Medal currentlyDrawnMedal;
        public void DrawMedals(float dt)
        {
            spriteBatch.Begin();

            // Draw each Medal
            if (currentlyDrawnMedal != null)
            {
                if (!currentlyDrawnMedal.Draw(dt))
                {
                    currentlyDrawnMedal = null;
                }
            }
            else
            {
                foreach (string medalTitle in medalList.Keys)
                {
                    if (medalList[medalTitle].Draw(dt))
                    {
                        currentlyDrawnMedal = medalList[medalTitle];
                        break;
                    }
                }
            }

            spriteBatch.End();
        }

        // PROPERTIES
        // GAMEPLAY STATS
        public int TotalScore
        {
            get { return totalScore; }
            set { totalScore = value; }
        }

        public int ShotsFired
        {
            get { return shotsFired; }
            set 
            {
                // Update selected weapon stats
                int difference = value - shotsFired;
                weaponRecords[selectedWeaponName + ".gun"].ShotsFired += difference;
                
                shotsFired = value;
            }
        }

        public int TargetHits
        {
            get { return targetsHit; }
            set 
            {
                // Update selected weapon stats
                int difference = value - targetsHit;
                weaponRecords[selectedWeaponName + ".gun"].TargetsHit += difference;

                targetsHit = value; 
            }
        }

        public int Multishots
        {
            get { return multishots; }
            set
            {
                // Update selected weapon stats
                int difference = value - multishots;
                weaponRecords[selectedWeaponName + ".gun"].Multishots += difference;

                multishots = value;
            }
        }

        public int Longshots
        {
            get { return longshots; }
            set 
            {
                // Update selected weapon stats
                int difference = value - longshots;
                weaponRecords[selectedWeaponName + ".gun"].Longshots += difference;

                longshots = value; 
            }
        }

        public int Snipershots
        {
            get { return snipershots; }
            set
            {
                // Update selected weapon stats
                int difference = value - snipershots;
                weaponRecords[selectedWeaponName + ".gun"].Snipershots += difference;

                snipershots = value;
            }
        }

        public int Bullseyes
        {
            get { return bullseyes; }
            set
            {
                // Update selected weapon stats
                int difference = value - bullseyes;
                weaponRecords[selectedWeaponName + ".gun"].Bullseyes += difference;

                bullseyes = value;
            }
        }

        public int Headshots
        {
            get { return headshots; }
            set
            {
                // Update selected weapon stats
                int difference = value - headshots;
                weaponRecords[selectedWeaponName + ".gun"].Headshots += difference;

                headshots = value;
            }
        }

        public int Knockdowns
        {
            get { return knockdowns; }
            set { knockdowns = value; }
        }

        public int Misses
        {
            get
            {
                return (shotsFired - targetsHit) < 0 ? 0 : shotsFired - targetsHit;
            }
        }

        public float Accuracy
        {
            get
            {
                if (shotsFired > 0)
                    return (float)targetsHit / (float)shotsFired;
                else
                    return 0.0f;
            }
        }

        public int MedalsUnlocked
        {
            get { return medalsUnlocked; }
        }

        public Dictionary<string, Medal> MedalList
        {
            get { return medalList; }
        }

        public Dictionary<string, LevelRecord> ScoreAttackRecords
        {
            get { return scoreAttackRecords; }
        }

        public Dictionary<string, LevelRecord> WeaponChallengeRecords
        {
            get { return weaponChallengeRecords; }
        }

        public Dictionary<String, WeaponRecord> WeaponRecords
        {
            get { return weaponRecords; }
        }

        public string SelectedWeaponName
        {
            get { return selectedWeaponName; }
            set { selectedWeaponName = value; }
        }

        // SETTINGS
        public bool IsLoggedIn
        {
            get { return isLoggedIn; }
        }

        public bool InvertCameraY
        {
            get { return invertCameraY; }
            set { invertCameraY = value; }
        }

        public float CameraSensitivityX
        {
            get { return cameraSensitivityX; }
            set { cameraSensitivityX = value; }
        }

        public float CameraSensitivityY
        {
            get { return cameraSensitivityY; }
            set { cameraSensitivityY = value; }
        }

        public bool MusicEnabled
        {
            get { return musicEnabled; }
            set
            {
                musicEnabled = value;

                // Set Media Player volume
                MediaPlayer.Volume = musicEnabled ? musicVolume : 0.0f;
            }
        }

        public float MusicVolume
        {
            get { return musicVolume; }
        }
    }
}