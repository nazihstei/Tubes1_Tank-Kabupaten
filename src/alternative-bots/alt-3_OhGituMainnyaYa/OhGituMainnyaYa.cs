using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.Encodings.Web;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;
using Robocode.TankRoyale.Schema;

// ----------------- Main Class: OhGituMainnyaYa -----------------
public class OhGituMainnyaYa : Bot {

    // CONSTANT
    private enum Mode {BRUTE_SCAN, LOCK_ON_TARGET};
    private Random rand = new Random();
    // Attribute
    private Mode BotMode;
    private EnemyBot TargetBot;
    private Dictionary<int, EnemyBot> ScannedBots;
    private HashSet<int> DeadBots;
    private bool isMovingForward; // for crazy move
    private int ErrorHitCount;
    private int HitByBotCount;


    // ----------------- Main to Start -----------------
    public static void Main(string[] args) {
        try {
            new OhGituMainnyaYa().Start();
        } catch (Exception ex) {
            Console.WriteLine($"Error di Main: {ex.Message}");
        }
    }
    public OhGituMainnyaYa() : base(BotInfo.FromFile("OhGituMainnyaYa.json")) {
        try {
            this.BotMode = Mode.BRUTE_SCAN;
            this.ScannedBots = new Dictionary<int, EnemyBot>();
            this.isMovingForward = true;
            this.DeadBots = new HashSet<int>();
            this.ErrorHitCount = 0;
            this.HitByBotCount = 0;
        } catch (Exception ex) {
            Console.WriteLine($"Error di Constructor: {ex.Message}");
        }
    }
    public override void OnGameStarted(GameStartedEvent e) {
        // Setup Color: chat gpt aja
        this.BodyColor  = Color.FromArgb(255, 10, 200, 150);   // Cyan kehijauan terang  
        this.TurretColor= Color.FromArgb(255, 220, 10, 180);   // Magenta neon  
        this.RadarColor = Color.FromArgb(255, 255, 140, 0);    // Jingga terang  
        this.BulletColor= Color.FromArgb(255, 0, 255, 255);    // Biru neon (Cyan)  
        this.ScanColor  = Color.FromArgb(255, 255, 0, 255);    // Ungu neon terang  
        this.GunColor   = Color.FromArgb(255, 0, 255, 100);    // Hijau neon  
        this.TracksColor= Color.FromArgb(255, 255, 255, 50);   // Kuning lemon pucat  
    }


    // ----------------- Main Algorithm -----------------
    public override void Run() {
        try {
            Console.WriteLine("Welcome to Mobile Legend");
            while (this.IsRunning) {
                if (this.BotMode == Mode.BRUTE_SCAN) {
                    this.BruteScan();
                } else {
                    this.LockOnTarget();
                }
                this.Go();
            }
        } catch (Exception ex) {
            Console.WriteLine($"Error di Run: {ex.Message}");
        }
    }
    // Brute Scan Mode
    public void BruteScan() {
        try {
            // Setting up properties
            this.ErrorHitCount = 0;
            this.HitByBotCount = 0;
            // Setting up gun
            if (this.GunDirection != this.Direction) {
                this.TurnGunLeft(this.CalcGunBearing(this.Direction));
            }
            this.AdjustRadarForGunTurn = true;

            // Scan musuh sebanyak 2 putaran
            this.TurnRadarLeft(360);
            if (this.TargetBot != null) {
                this.TurnRadarLeft(this.RadarBearingTo(this.TargetBot.X, this.TargetBot.Y));
            }
            // Mengambil musuh yang paling terpencil
            if (this.ScannedBots.Count > 0) {
                (double, double) MeanPosition = this.CalcMeanPosition();
                Func<EnemyBot, double> DeltaPosition = (bot) => Math.Sqrt(Math.Pow(bot.X-MeanPosition.Item1, 2) + Math.Pow(bot.Y-MeanPosition.Item2, 2));
                this.TargetBot = this.ScannedBots.Values.OrderBy(bot => DeltaPosition(bot)).ThenBy(bot => bot.energy).First();
                this.BotMode = Mode.LOCK_ON_TARGET;
                if (this.GunDirection != this.RadarDirection) {
                    this.TurnRadarLeft(this.CalcRadarBearing(this.GunDirection));
                }
                this.AdjustRadarForGunTurn = false;
            } else {
                // mencegah serangan gratis
                this.RunCrazyMove(( this.ArenaWidth/2, this.ArenaHeight/2), 
                                    Math.Min(this.ArenaHeight/4, this.ArenaWidth/4)); 
            }
            this.Go();
        } catch (Exception ex) {
            Console.WriteLine($"Error di BruteScan: {ex.Message}");
        }
    }
    // Lock On Target Mode
    public void LockOnTarget() {
        try {
            // Evaluasi Isolation Score terbaru
            if (this.HitByBotCount > 5) {
                this.BotMode = Mode.BRUTE_SCAN;
            }
            // Cek eksistensi target
            if (this.TargetBot == null) { // target loss
                this.BotMode = Mode.BRUTE_SCAN;
            // Still on target
            } else {
                this.RunCrazyMove(( this.TargetBot.X, this.TargetBot.Y), 
                                    Math.Min(this.ArenaWidth/4, this.ArenaHeight/4));
                double firepower = this.CalculateFirepower();
                this.LockGun(firepower);
                this.SetFire(firepower);
            }
            this.Go();
        } catch (Exception ex) {
            Console.WriteLine($"Error di LockOnTarget: {ex.Message}");
        }
    }
    

    // ----------------- Event Handler -----------------
    public override void OnScannedBot(ScannedBotEvent e) {
        try {
            // Console.WriteLine($"I see a bot {e.ScannedBotId} at ({e.X}, {e.Y}).");
            // Check dead bots
            if (! this.DeadBots.Contains(e.ScannedBotId)) {
                // while brute scan mode
                if (this.BotMode == Mode.BRUTE_SCAN) {
                    try {
                        this.ScannedBots.Add(e.ScannedBotId, new EnemyBot(this, e));
                    } catch {
                        this.ScannedBots[e.ScannedBotId] = new EnemyBot(this, e);
                    }
                // while lock on target mode
                } else if (this.TargetBot.id == e.ScannedBotId) {
                    this.TargetBot = new EnemyBot(this, e);
                } else {
                    ; // do nothing
                }
            }
            this.Rescan();
        } catch (Exception ex) {
            Console.WriteLine($"Error di OnScannedBot: {ex.Message}");
        }
    }
    public override void OnBotDeath(BotDeathEvent e) {
        try {
            Console.WriteLine($"Bot {e.VictimId} mati. TargetBot sebelum: {this.TargetBot?.id}");
            if (this.TargetBot != null && this.TargetBot.id == e.VictimId) {
                this.TargetBot = null;
                this.BotMode = Mode.BRUTE_SCAN;
                this.ScannedBots.Clear();
                this.isMovingForward = true;
                this.DeadBots.Add(e.VictimId);
            }
        } catch (Exception ex) {
            Console.WriteLine($"Error di OnBotDeath: {ex.Message}");
        }
    }
    public override void OnRoundStarted(RoundStartedEvent e) {
        try {
            this.BotMode = Mode.BRUTE_SCAN;
            this.TargetBot = null;
            this.ScannedBots.Clear();
            this.isMovingForward = true;
            this.DeadBots.Clear();
        } catch (Exception ex) {
            Console.WriteLine($"Error di OnRoundStarted: {ex.Message}");
        }
    }
    public override void OnBulletHitWall(BulletHitWallEvent e) {
        this.ErrorHitCount++;
    }
    public override void OnBulletHit (BulletHitBotEvent e) {
        this.ErrorHitCount = 0;
    }
    public override void OnHitByBullet(HitByBulletEvent e) {
        this.HitByBotCount++;
    }

    // ----------------- Additional Method -----------------
    public void LockGun(double firepower) {
        try {
            if (this.TargetBot != null) {

                // Evaluasi presisi posisi bot
                if (this.ErrorHitCount > 3) {
                    this.TurnGunLeft(360);
                    this.ErrorHitCount = 0;
                }
                this.AdjustGunForBodyTurn = true;
                this.SetGunPrecision(firepower);
                this.AdjustGunForBodyTurn = false;
            }
        } catch (Exception ex) {
            Console.WriteLine($"Error di LockGun: {ex.Message}");
        }
    }
    public void RunCrazyMove((double x, double y) axis, double radius) {
        try {
            // Ubah kecepatan secara dinamis untuk membuat pergerakan tidak terduga
            this.TargetSpeed = 2 + rand.NextDouble() * 6; // Speed antara 2 - 8
            // Perubahan arah tiba-tiba secara acak
            if ((int)(this.X + this.Y) % 3 == 0) {
                this.isMovingForward = !this.isMovingForward;
                this.SetTurnLeft(rand.Next(-90, 90));
            }
            // Jika keluar dari batas, putar kembali ke tengah
            double distance = this.DistanceTo(axis.x, axis.y);
            double margin = Math.Max(radius/2, 100);
            if (distance > radius) {
                this.SetTurnLeft(this.BearingTo(axis.x, axis.y));
                this.isMovingForward = true;
            } else if (distance < margin) {
                this.SetTurnRight(this.BearingTo(axis.x, axis.y));
                this.isMovingForward = true;
            }
            // Ubah arah gerakan agar sulit ditebak
            if (this.isMovingForward) {
                this.SetForward(100);
            } else {
                this.SetBack(100);
            }
        } catch (Exception ex) {
            Console.WriteLine($"Error di RunCrazyMove: {ex.Message}");
        }
    }
    public double CalculateFirepower() {
        try {
            return Math.Min(3, 400/this.TargetBot.distance);
        } catch (Exception ex) {
            Console.WriteLine($"Error di CalculateFirepower: {ex.Message}");
            return -1;
        }
    }
    public (double X, double Y) CalcMeanPosition() {
        try {
            if (this.ScannedBots.Count == 0) {
                return (0, 0);
            } else {
                double sumX = 0; double sumY = 0;
                foreach (EnemyBot bot in this.ScannedBots.Values) {
                    sumX += bot.X;
                    sumY += bot.Y;
                }
                double meanX = sumX / this.ScannedBots.Count;
                double meanY = sumY / this.ScannedBots.Count;
                return (meanX, meanY);
            }
        } catch (Exception ex) {
            Console.WriteLine($"Error fi CalcMeanPosition: {ex.Message}");
            return (-1, -1);
        }
    }
    public void SetGunPrecision(double firepower) {
        try {
            // Hitung waktu tempuh peluru
            double bulletSpeed = this.CalcBulletSpeed(firepower);
            double distance = this.DistanceTo(this.TargetBot.X, this.TargetBot.Y);
            double timeToHit = distance / bulletSpeed;
            // Prediksi posisi musuh berdasarkan kecepatan dan arah geraknya
            double predictedX = this.TargetBot.X + Math.Cos(this.TargetBot.direction * Math.PI/180) * this.TargetBot.speed * timeToHit;
            double predictedY = this.TargetBot.Y + Math.Sin(this.TargetBot.direction * Math.PI/180) * this.TargetBot.speed * timeToHit;
            // Hitung bearing ke posisi yang diprediksi
            double predictedBearing = this.GunBearingTo(predictedX, predictedY);
            this.TurnGunLeft(predictedBearing);
        } catch (Exception ex) {
            Console.WriteLine($"Error di SetGunPrecision: {ex.Message}");
        } 
    }
}

/*  Keterangan mengenai Bot
    A. definition and setup
    1. pada algoritma ini, bot memiliki 2 mode yang non-interuptable, yakni brute scan mode dan lock on target mode. 
    2. brute scan mode adalah mode untuk menentukan bot musuh yang akan menjadi target di lock on target mode.

    B. brute scan mode
    1. pada kondisi start, bot akan melakukan pemindaian 360 derajat.
    2. selama pemindaian, bot mengkomparasi setiap lokasi dari bot yang dipindainya. bot musuh dengan lokasi paling terpencil dari semua bot yang dipindai, memiliki prioritas paling tinggi. 
    3. selama pemindaian, bot melakukan gerakan di area sekitar tengah dengan gerakan melingkar atau membentuk suatu pola tertentu agar tidak diam (silahkan pilih, jangan lupa untuk memainkan velocity dan arah gerak yang cukup "crazy"), dengan catatan, pola tersebut harus berada di sekitar tengah arena. pada mode ini, radar terpisah dari gun dan body agar dapat memindai dengan cepat.
    4. setelah menentukan bot dengan prioritas tertinggi, bot memasuki lock on target mode. 

    C. lock on target mode
    1. pada mode ini, bot hanya tertuju kepada satu bot yang sudah dipilih pada brute scan mode.
    2. body terpisah dari gun dan radar, gun dan radar saling menyatu dan selalu mengarah ke arah yang sama. 
    3. bot akan selalu melakukan pergerakan dengan pola tertentu di area sekitar bot target (boleh menggunakan pola yang sama dengan brute scan mode, maupun berbeda), sementara gun dan radar selalu mengarah ke target tanpa peduli sepert apa posisi dan arah gerak bot kita saat ini.  
    4. perbedaan crazy move antara brute scan mode dan lock on target mode, adalah titik pusanya. brute scan mode memiliki titik pusat tepat di tengah arena, dan tidak boleh keluar dari jangkauan tertentu. sedangkan, lock on target mode, memiliki titik pusat berupa titik pada jarak tertentu dari target sedemikian sehingga radius batas pola gerakan tidak menyentuh wall.
    5. bot akan menembak ke target apabila gunheat=0. pikirkan baik-baik "error value" akibat gerakan bot kita, maupun gerakan bot target. pikirkan pula firepower yang harus dibuat dengan mempertimbangkan jarak, kecepatan bullet, dan energy risk. 
    6. bot akan keluar dari lock on target mode dan masuk ke brute scan mode untuk mengevaluasi posisi terbaru dari bot-bot musuh apabila bot kita tertembak oleh bot lain sebanyak 5x.
*/