using System.Text.Json;

namespace Sparta_Dungeon
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.Write(" 플레이어 이름을 입력하세요 : ");
            string name = Console.ReadLine();

            Item[] items = new Item[8]; ;
            {
                string[] names =
                {
                    "수련자 갑옷",
                    "무쇠 갑옷",
                    "스파르타의 갑옷",
                    "용사의 갑옷",
                    "낡은 검",
                    "청동 도끼",
                    "스파르타의 창",
                    "용사의 성검"
                };
                string[] descriptions =
                    {
                    "수련에 도움을 주는 갑옷입니다.",
                    "무쇠로 만들어진 튼튼한 갑옷입니다.",
                    "스파르타의 전사들이 사용했다는 전설의 갑옷입니다.",
                    "용사가 사용했다는 무적의 갑옷입니다.",
                    "쉽게 볼 수 있는 낡은 검입니다.",
                    "어디선가 사용됐던거 같은 도끼입니다.",
                    "스파르타의 전사들이 사용했다는 전설의 창입니다.",
                    "용사가 사용했다는 최강의 검입니다."
                };
                int[] stats = { 5, 9, 15, 1000, 2, 5, 7, 500 };
                int[] prices = { 1000, 2100, 3500, 100000, 600, 1500, 3200, 100000 };

                for (int i = 0; i < items.Length; i++)
                {
                    items[i] = new Item(names[i], stats[i], prices[i], descriptions[i], i < 4 ? false : true);
                }
            }
            Character player = SaveSystem.Load(name, items);

            if (player == null)
            {
                Console.WriteLine(" 새 플레이어 생성 중...\n");

                Console.WriteLine(" 1. 전사");
                Console.WriteLine(" 2. 도적\n");

                Console.Write(" 직업을 선택하세요 :");

                bool isSelected = false;
                Job job = Job.warrior;
                while (!isSelected)
                {
                    string jobName = Console.ReadLine();
                    switch (jobName)
                    {
                        case "1":
                            job = Job.warrior;
                            isSelected = true;
                            break;

                        case "2":
                            job = Job.rogue;
                            isSelected = true;
                            break;

                        default:
                            Console.WriteLine("다시 입력하세요: ");
                            break;
                    }
                }
                player = new Character(name, job);
            }

            Screen? current = TownScreen.instance;
            while(current != null)
            {
                current.player = player;
                current.items = items;
                current.Show();
                current = current.Next();
            }
        }
    }

    /////////////////////////////////////////////////////////////////////////////////////
    // 게임 매니저
    /////////////////////////////////////////////////////////////////////////////////////
    public class SaveData
    {
        public string Name { get; set; }
        public Job Job { get; set; }
        public int Level { get; set; }
        public int Health { get; set; }
        public float Attack { get; set; }
        public int Defense { get; set; }
        public int Gold { get; set; }
        public int Exp { get; set; }
        public List<SaveItem> Items { get; set; }
    }

    public class SaveItem
    {
        public string Name { get; set; }
        public bool Sold { get; set; }
        public bool IsEquip { get; set; }
    }

    public static class SaveSystem
    {
        public static void Save(Character player, Item[] items)
        {
            Status status = player.GetStatus();

            SaveData data = new SaveData
            {
                Name = status.userName,
                Job = status.job,
                Level = status.level,
                Health = status.health,
                Attack = status.attack,
                Defense = status.defense,
                Gold = player.gold,
                Exp = player.exp,
                Items = items.Select(i => new SaveItem
                {
                    Name = i.name,
                    Sold = i.sold,
                    IsEquip = i.isEquip
                }).ToList()
            };
            
            string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText($"{data.Name}.json", json);
        }

        public static Character Load(string name, Item[] itemTemplate)
        {
            string path = $"{name}.json";
            if (!File.Exists(path)) return null;

            try
            {
                string json = File.ReadAllText(path);
                SaveData data = JsonSerializer.Deserialize<SaveData>(json);

                Character player = new Character(data.Name, data.Job);
                player.gold = data.Gold;
                player.exp = data.Exp;
                player.SetStatus(data.Level, data.Health, data.Attack, data.Defense);

                foreach (var savedItem in data.Items)
                {
                    var item = itemTemplate.FirstOrDefault(i => i.name == savedItem.Name);
                    if (item != null)
                    {
                        item.sold = savedItem.Sold;
                        item.isEquip = savedItem.IsEquip;
                    }
                }

                return player;
            }
            catch (Exception e)
            {
                Console.WriteLine(" 저장 파일 불러오는 중 오류 발생:");
                Console.WriteLine(e.Message);
                return null;
            }
        }
    }
    /////////////////////////////////////////////////////////////////////////////////////
    // 창 관련 기능
    /////////////////////////////////////////////////////////////////////////////////////
    abstract class Screen
    {
        public Character player;
        public Item[] items;
        protected bool isRetry = false;

        public void PrintItems(bool isNum, bool isInPlayer, bool isInven)
        {
            int i = 1;
            foreach (Item item in items)
            {
                string attackOrDefense = item.isWeapon ? "공격력" : "방어력";
                string price = item.sold ? "구매완료" : item.price.ToString() + " G";

                if (isInPlayer)
                {
                    if(!item.sold) continue;
                    price = item.reCellPrice.ToString() + " G";
                }

                if (isInven)    Console.Write("\t\t\t|\t\t|");
                else            Console.Write("\t\t\t|\t\t|\t\t\t\t\t\t\t|");

                Console.SetCursorPosition(0, Console.GetCursorPosition().Top);
                Console.Write(" - {0}{1}{2}",
                    isNum ? i.ToString() + " " : "", isInven && item.isEquip ? "[E]" : "", item.name);

                Console.SetCursorPosition(25, Console.GetCursorPosition().Top);
                Console.Write(" {0} +{1}", attackOrDefense, item.raiseStat);

                Console.SetCursorPosition(41, Console.GetCursorPosition().Top);
                Console.Write(" {0}{1}", item.description, isInven ? "\n" : "");

                if (!isInven)
                {
                    Console.SetCursorPosition(97, Console.GetCursorPosition().Top);
                    Console.WriteLine(" {0}", price);
                }

                i++;
            }
        }

        public void PrintTitle(string title)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine(title);
            Console.ResetColor();
        }

        public void PrintUserInstruction()
        {
            if (!isRetry)
                Console.Write(" 원하시는 행동을 입력해주세요 : ");
            else
                Console.Write(" 잘못된 입력입니다! 다시 입력해주세요 : ");

            isRetry = false;
        }

        public void GameOver()
        {
            Console.SetCursorPosition(0, Console.GetCursorPosition().Top);
            Console.Write(new String(' ', Console.BufferWidth));
            Console.SetCursorPosition(0, Console.GetCursorPosition().Top - 2);
            Console.WriteLine(" 체력이 0이 되어 사망하였습니다....\n");

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(" 게임 오버");
            Console.ResetColor();
        }

        public abstract void Show();
        public abstract Screen? Next();
    }

    class TownScreen : Screen
    {
        public static readonly TownScreen instance = new TownScreen();
        private TownScreen() { }

        public override void Show()
        {
            Console.Clear();

            Console.WriteLine(" 스파르타 마을에 오신 여러분 환영합니다.");
            Console.WriteLine(" 이곳에서 던전으로 들어가기전 활동을 할 수 있습니다.");

            Console.WriteLine("\n 1. 상태 보기");
            Console.WriteLine(" 2. 인벤토리");
            Console.WriteLine(" 3. 상점");
            Console.WriteLine(" 4. 던전입장");
            Console.WriteLine(" 5. 휴식하기");
            Console.WriteLine(" 0. 게임 종료\n");

            PrintUserInstruction();
        }

        public override Screen? Next()
        {
            string input = Console.ReadLine();
            switch (input)
            {
                case "1": return StatusScreen.instance;
                case "2": return InventoryScreen.instance;
                case "3": return ShopScreen.instance;
                case "4": return DungeonScreen.instance;
                case "5": return new RestScreen();
                case "0": SaveSystem.Save(player, items); return null;
                default:
                    isRetry = true;
                    return this;
            }
        }
    }

    class StatusScreen : Screen
    {
        public static readonly StatusScreen instance = new StatusScreen();
        private StatusScreen() { }

        public override void Show()
        {
            Console.Clear();

            PrintTitle(" 상태 보기");

            Console.WriteLine("\n 캐릭터의 정보가 표시됩니다.\n");

            player.ShowCharacterInfo();

            Console.WriteLine("\n 0. 나가기\n");

            PrintUserInstruction();
        }

        public override Screen? Next()
        {
            string input = Console.ReadLine();
            switch (input)
            {
                case "0": return TownScreen.instance;
                default:
                    isRetry = true;
                    return this;
            }
        }
    }

    class InventoryScreen : Screen
    {
        public static readonly InventoryScreen instance = new InventoryScreen();
        private InventoryScreen() { }

        public override void Show()
        {
            Console.Clear();

            PrintTitle(" 인벤토리");

            Console.WriteLine("\n 보유 중인 아이템을 관리할 수 있습니다.\n");

            Console.WriteLine(" [아이템 목록]");
            PrintItems(false, true, true);

            Console.WriteLine("\n 1. 장착 관리");
            Console.WriteLine(" 0. 나가기\n");

            PrintUserInstruction();
        }

        public override Screen? Next()
        {
            string input = Console.ReadLine();
            switch (input)
            {
                case "1": return EquipmentScreen.instance;
                case "0": return TownScreen.instance;
                default:
                    isRetry = true;
                    return this;
            }
        }
    }

    class EquipmentScreen : Screen
    {
        public static readonly EquipmentScreen instance = new EquipmentScreen();
        private EquipmentScreen() { }

        public override void Show()
        {
            Console.Clear();

            PrintTitle(" 인벤토리 - 장착 관리");

            Console.WriteLine("\n 보유 중인 아이템을 관리할 수 있습니다.\n");

            Console.WriteLine(" [아이템 목록]");
            PrintItems(true, true, true);

            Console.WriteLine("\n 0. 나가기\n");

            PrintUserInstruction();
        }

        public override Screen? Next()
        {
            string input = Console.ReadLine();
            if (input == "0") 
            {
                isRetry = false;
                return InventoryScreen.instance; 
            }

            int.TryParse(input, out int num);
            if (num == 0) num--;
            foreach (Item item in items)
            {
                if (item.sold)
                {
                    num--;
                    if (num == 0)
                    {
                        if (item.isEquip)
                        {
                            item.isEquip = false;
                            if(item.isWeapon) player.attackOffset = 0;
                            else player.defenseOffset = 0;
                        }
                        else
                        {
                            foreach(Item release in items)
                            {
                                if(release.isEquip && item.isWeapon == release.isWeapon)
                                    release.isEquip = false;
                            }

                            item.isEquip = true;
                            if (item.isWeapon) player.attackOffset = item.raiseStat;
                            else player.defenseOffset = item.raiseStat;
                        }
                        break;
                    }
                }
            }

            if (num != 0)
            {
                isRetry = true;
            }

            return this;
        }
    }

    class ShopScreen : Screen
    {
        public static readonly ShopScreen instance = new ShopScreen();
        private ShopScreen() { }

        public override void Show()
        {
            Console.Clear();

            PrintTitle(" 상점");

            Console.WriteLine("\n 필요한 아이템을 얻을 수 있는 상점입니다.\n");

            Console.WriteLine(" [보유 골드]");
            Console.WriteLine($" {player.gold} G\n");

            Console.WriteLine(" [아이템 목록]");
            PrintItems(false, false, false);

            Console.WriteLine("\n 1. 아이템 구매");
            Console.WriteLine(" 2. 아이템 판매");
            Console.WriteLine(" 0. 나가기\n");

            PrintUserInstruction();
        }

        public override Screen? Next()
        {
            string input = Console.ReadLine();
            switch (input)
            {
                case "1": return BuyScreen.instance;
                case "2": return CellScreen.instance;
                case "0": return TownScreen.instance;
                default:
                    isRetry = true;
                    return this;
            }
        }
    }

    class BuyScreen : Screen
    {
        public static readonly BuyScreen instance = new BuyScreen();
        private BuyScreen() { }

        private int flag = -1;

        public override void Show()
        {
            Console.Clear();

            PrintTitle(" 상점 - 아이템 구매");

            Console.WriteLine("\n 필요한 아이템을 얻을 수 있는 상점입니다.\n");

            Console.WriteLine(" [보유 골드]");
            Console.WriteLine($" {player.gold} G\n");

            Console.WriteLine(" [아이템 목록]");
            PrintItems(true, false, false);

            switch (flag)
            {
                case 1: // 구매한 아이템일 경우
                    Console.WriteLine("\n 이미 구매한 아이템입니다.");
                    break;

                case 2: // 구매를 했을 경우
                    Console.WriteLine("\n 구매를 완료했습니다.");
                    break;

                case 3: // 골드가 부족할 경우
                    Console.WriteLine("\n 골드가 부족합니다.");
                    break;

                default:
                    break;
            }

            Console.WriteLine("\n 0. 나가기\n");

            PrintUserInstruction();
        }

        public override Screen? Next()
        {
            string input = Console.ReadLine();
            if(input == "0")
            {
                flag = -1;
                isRetry = false;
                return ShopScreen.instance;
            }

            if (int.TryParse(input, out int num) && 0 < num && num <= items.Length)
            {
                num--;

                if (items[num].sold) flag = 1;
                else if (items[num].price > player.gold)
                {
                    flag = 3;
                }
                else
                {
                    flag = 2;
                    items[num].sold = true;
                    player.gold -= items[num].price;
                }
            }
            else
            {
                flag = -1;
                isRetry = true;
            }

            return this;
        }
    }

    class CellScreen : Screen
    {
        public static readonly CellScreen instance = new CellScreen();
        private CellScreen() { }

        private bool flag = false;
        private Item? removedItem;

        public override void Show()
        {
            Console.Clear();

            PrintTitle(" 상점 - 아이템 판매");

            Console.WriteLine("\n 필요한 아이템을 얻을 수 있는 상점입니다.\n");

            Console.WriteLine(" [보유 골드]");
            Console.WriteLine($" {player.gold} G\n");

            Console.WriteLine(" [아이템 목록]");
            PrintItems(true, true, false);

            if(flag && removedItem != null) Console.WriteLine($"\n {removedItem.name}을/를 판매하였습니다.");

            Console.WriteLine("\n 0. 나가기\n");

            PrintUserInstruction();
        }

        public override Screen? Next()
        {
            string input = Console.ReadLine();
            if (input == "0")
            {
                isRetry = false;
                flag = false;
                return ShopScreen.instance;
            }

            int.TryParse(input, out int num);
            if (num == 0) num--;

            foreach (Item item in items)
            {
                if (item.sold)
                {
                    num--;
                    if (num == 0)
                    {
                        if (item.isEquip)
                        {
                            item.isEquip = false;
                            if(item.isWeapon) player.attackOffset = 0;
                            else player.defenseOffset = 0;
                        }
                        item.sold = false;
                        player.gold += item.reCellPrice;
                        removedItem = item;
                        flag = true;
                        break;
                    }
                }
            }

            if (num != 0)
            {
                flag = false;
                isRetry = true;
            }

            return this;
        }
    }

    public enum DungeonStage { Easy, Normal, Hard }
    class DungeonScreen : Screen
    {
        public static readonly DungeonScreen instance = new DungeonScreen();
        private DungeonScreen() { }

        private Random rand = new Random();

        public bool DungeonResult(DungeonStage stage)
        {
            switch (stage)
            {
                case DungeonStage.Easy:
                    if(player.GetStatValue(Stat.DEF) >= 5) return true;
                    break;
                        
                case DungeonStage.Normal:
                    if (player.GetStatValue(Stat.DEF) >= 11) return true;
                    break;

                case DungeonStage.Hard:
                    if (player.GetStatValue(Stat.DEF) >= 17) return true;
                    break;
            }

            return rand.Next(1, 101) > 40 ? true : false;
        }

        public override void Show()
        {
            Console.Clear();

            PrintTitle(" 던전입장");

            Console.WriteLine("\n 이곳에서 던전으로 들어가기 전 활동을 할 수 있습니다.\n");

            Console.WriteLine(" 1. 쉬운 던전\t| 방어력 5 이상 권장");
            Console.WriteLine(" 2. 일반 던전\t| 방어력 11 이상 권장");
            Console.WriteLine(" 3. 어려운 던전\t| 방어력 17 이상 권장");
            Console.WriteLine(" 0. 나가기\n");

            PrintUserInstruction();
        }

        public override Screen? Next()
        {
            string input = Console.ReadLine();
            switch (input)
            {
                case "1": 
                    return new ResultScreen(DungeonResult(DungeonStage.Easy), DungeonStage.Easy);

                case "2": 
                    return new ResultScreen(DungeonResult(DungeonStage.Normal), DungeonStage.Normal);

                case "3": 
                    return new ResultScreen(DungeonResult(DungeonStage.Hard), DungeonStage.Hard);

                case "0": 
                    return TownScreen.instance;

                default:
                    isRetry = true;
                    return this;
            }
        }
    }

    class ResultScreen : Screen
    {
        public ResultScreen(bool _isWin, DungeonStage _stage)
        {
            isWin = _isWin;
            stage = _stage;
            switch (stage)
            {
                case DungeonStage.Easy:
                    stageName = "쉬운 던전"; break;

                case DungeonStage.Normal:
                    stageName = "일반 던전"; break;

                case DungeonStage.Hard:
                    stageName = "어려운 던전"; break;

                default:
                    stageName = ""; break;
            }
        }

        private bool isWin;
        private DungeonStage stage;
        private string stageName;

        private int damage;
        private int reward;
        private bool isLevelUp;

        public override void Show()
        {
            Console.Clear();

            PrintTitle(" 던전 결과");

            if (isWin)
            {
                Console.WriteLine("\n 축하합니다!!\n");
                Console.WriteLine($" {stageName}을 클리어 하였습니다.\n");

                Console.WriteLine(" [탐험 결과]");
                if(!isRetry) player.DungeonClear(stage, out damage, out reward, out isLevelUp);
                Console.WriteLine($" 체력 : {player.GetStatValue(Stat.hp) + damage} -> {player.GetStatValue(Stat.hp)}");
                Console.WriteLine($" 골드 : {player.gold - reward} G -> {player.gold} G");
                if (isLevelUp) Console.WriteLine($" 레벨 : {player.GetStatValue(Stat.level) - 1} -> {player.GetStatValue(Stat.level)}");
            }
            else
            {
                Console.WriteLine("\n 이럴수가;;");
                Console.WriteLine($" {stageName} 클리어에 실패하였습니다.\n");

                Console.WriteLine(" [탐험 결과]");
                if (!isRetry)  player.DungeonFailed(out damage);
                Console.WriteLine($" 체력 : {player.GetStatValue(Stat.hp) + damage} -> {player.GetStatValue(Stat.hp)}");
            }
            
            Console.WriteLine("\n 0. 나가기\n");

            PrintUserInstruction();
        }

        public override Screen? Next()
        {
            if (!player.GetLive())
            {
                GameOver();
                return null;
            }

            string input = Console.ReadLine();
            switch (input)
            {
                case "0": return TownScreen.instance;
                default:
                    isRetry = true;
                    return this;
            }
        }
    }

    class RestScreen : Screen
    {
        public RestScreen() { isRest = false; }
        
        private bool isRest;
        private int flag = -1;

        public override void Show()
        {
            Console.Clear();

            PrintTitle(" 휴식하기");

            Console.WriteLine($"\n 500 G 를 내면 체력을 회복할 수 있습니다. (보유 골드 : {player.gold} G)\n");

            switch (flag)
            {
                case 1: Console.WriteLine(" 휴식을 완료했습니다.\n"); break;
                case 2: Console.WriteLine(" 골드가 부족합니다.\n"); break;
                default: break;
            }

            if(flag == -1) Console.WriteLine(" 1. 휴식하기");
            Console.WriteLine(" 0. 나가기\n");

            PrintUserInstruction();
        }

        public override Screen? Next()
        {
            string input = Console.ReadLine();
            if (input == "0") 
            {
                isRetry = false;
                isRest = false;
                flag = -1;
                return TownScreen.instance; 
            }

            int.TryParse(input, out int num);
            if(num == 1 && !isRest)
            {
                if(player.gold >= 500)
                {
                    isRest = true;
                    player.gold -= 500;
                    player.SetFullHealth();
                    flag = 1;
                }
                else
                {
                    isRest = true;
                    flag = 2;
                }
            }
            else
            {
                isRetry = true;
            }

            return this;
        }
    }

    /////////////////////////////////////////////////////////////////////////////////////
    // 캐릭터 관련 기능
    /////////////////////////////////////////////////////////////////////////////////////
    public enum Job { warrior=1, rogue };
    public enum Stat { level, hp, DEF };

    public class Status
    {
        public Job job;

        public string userName;
        private string jobName;

        public int level;
        public int health;
        public float attack;
        public int defense;

        public bool isLive;

        public Status(string _userName, Job _job)
        {
            userName = _userName;
            level = 1;
            isLive = true;
            job = _job;

            switch (job)
            {
                case Job.warrior:
                    jobName = "전사";
                    attack = 10f;
                    defense = 5;
                    health = 100;
                    break;

                case Job.rogue:
                    jobName = "도적";
                    attack = 12f;
                    defense = 3;
                    health = 100;
                    break;
            }
        }

        public void ShowStatus(int ATKoffset, int DEFoffset)
        {
            string ATKstr = "(+" + ATKoffset + ")";
            string DEFstr = "(+" + DEFoffset + ")";
            Console.WriteLine($" Lv. {level:D2}");
            Console.WriteLine($" {userName} ( {jobName} )");
            Console.WriteLine(" 공격력 : {0} {1}", attack + ATKoffset, ATKoffset > 0 ? ATKstr : "");
            Console.WriteLine(" 방어력 : {0} {1}", defense + DEFoffset, DEFoffset > 0 ? DEFstr : "");
            Console.WriteLine($" 체  력 : {health}");
        }

        public void LevelUp()
        {
            level++;
            attack += 0.5f;
            defense += 1;
        }

        public int GetDamage(int damage)
        {
            if(health <= damage)
            {
                damage = health;
                health = 0;
                isLive = false;
            }
            else
            {
                health -= damage;
            }

            return damage;
        }
    }

    public class Character
    {
        private Status status;
        public int exp = 0;

        public int gold;
        public int attackOffset = 0;
        public int defenseOffset = 0;

        private Random rand = new Random();

        public Character(string _name, Job _job)
        {
            status = new Status(_name, _job);
            gold = 1500;
        }

        public void ShowCharacterInfo()
        {
            status.ShowStatus(attackOffset, defenseOffset);
            Console.WriteLine($" 골  드 : {gold} G");
        }

        public void SetFullHealth()
        {
            status.health = 100;
        }

        public int GetStatValue(Stat flag)
        {
            switch (flag)
            {
                case Stat.level: return status.level;
                case Stat.hp: return status.health;
                case Stat.DEF: return status.defense + defenseOffset;
                default: return -1;
            }
        }

        public void DungeonClear(DungeonStage stage, out int damage, out int reward, out bool isLevelup)
        {
            // 체력 계산
            int ignoreDefense = 0;
            switch (stage)
            {
                case DungeonStage.Easy:
                    ignoreDefense = 5 - (status.defense + defenseOffset);
                    break;

                case DungeonStage.Normal:
                    ignoreDefense = 11 - (status.defense + defenseOffset);
                    break;

                case DungeonStage.Hard:
                    ignoreDefense = 17 - (status.defense + defenseOffset);
                    break;
            }
            damage = status.GetDamage(
                rand.Next(20 + (ignoreDefense < -20 ? -20 : ignoreDefense), 36 + (ignoreDefense < -35 ? -35 : ignoreDefense)));

            // 골드 계산
            float bonusFactor = rand.Next((int)((status.attack + attackOffset) * 10), (int)((status.attack + attackOffset) * 20 + 1)) / 1000f;
            switch (stage)
            {
                case DungeonStage.Easy:
                    reward = (int)(1000 * (1 + bonusFactor));
                    break;

                case DungeonStage.Normal:
                    reward = (int)(1700 * (1 + bonusFactor));
                    break;

                case DungeonStage.Hard:
                    reward = (int)(2500 * (1 + bonusFactor));
                    break;

                default: reward = 0; break;
            }
            reward = (int)(MathF.Round(reward / 100f)) * 100;
            gold += reward;

            // 레벨 계산
            exp++;
            if (status.level == exp)
            {
                exp = 0;
                status.LevelUp();
                isLevelup = true;
            }
            else isLevelup = false;
        }

        public void DungeonFailed(out int damage)
        {
            damage = status.GetDamage(50);
        }

        public bool GetLive()
        {
            return status.isLive;
        }

        public Status GetStatus() => status;
        public void SetStatus(int lvl, int hp, float atk, int def)
        {
            status.level = lvl;
            status.health = hp;
            status.attack = atk;
            status.defense = def;
        }
    }

    /////////////////////////////////////////////////////////////////////////////////////
    // 아이템 관련 기능
    /////////////////////////////////////////////////////////////////////////////////////

    public class Item
    {
        public string name;
        public int raiseStat;
        public int price;
        public string description;
        public bool isWeapon;

        public bool sold = false;
        public int reCellPrice;

        public bool isEquip = false;

        public Item(string _name, int _raiseStat, int _price, string _description, bool _isWeapon)
        {
            name = _name;
            raiseStat = _raiseStat;
            price = _price;
            description = _description;
            isWeapon = _isWeapon;

            reCellPrice = (int)MathF.Round(price * 0.85f * 0.01f) * 100;
        }
    }
}
