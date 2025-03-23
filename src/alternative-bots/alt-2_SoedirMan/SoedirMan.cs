using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.Encodings.Web;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;
using Robocode.TankRoyale.Schema;

// ----------------- Main Class: SoedirMan -----------------
public class SoedirMan : Bot {

    // CONSTANT
    private enum Mode {BRUTE_SCAN, LOCK_ON_TARGET};
    private Random rand = new Random();
    // Attribute
    private Mode BotMode;
    private EnemyBot TargetBot;
    private Dictionary<int, EnemyBot> ScannedBots;
    private HashSet<int> DeadBots;
    private bool isMovingForward; // for crazy move
    private int ErrorCount;


    // ----------------- Main to Start -----------------
    public static void Main(string[] args) {
        try {
            new SoedirMan().Start();
        } catch (Exception ex) {
            Console.WriteLine($"Error di Main: {ex.Message}");
        }
    }
    public SoedirMan() : base(BotInfo.FromFile("SoedirMan.json")) {
        try {
            this.BotMode = Mode.BRUTE_SCAN;
            this.ScannedBots = new Dictionary<int, EnemyBot>();
            this.isMovingForward = true;
            this.DeadBots = new HashSet<int>();
            this.ErrorCount = 0;
        } catch (Exception ex) {
            Console.WriteLine($"Error di Constructor: {ex.Message}");
        }
    }
    public override void OnGameStarted(GameStartedEvent e) {
        // Setup Color: chat gpt aja
        this.BodyColor  = Color.FromArgb(255, 50, 50, 200);     // Biru tua
        this.TurretColor= Color.FromArgb(255, 200, 50, 50);     // Merah terang
        this.RadarColor = Color.FromArgb(255, 50, 200, 50);     // Hijau terang
        this.BulletColor= Color.FromArgb(255, 255, 215, 0);     // Emas
        this.ScanColor  = Color.FromArgb(255, 128, 0, 128);     // Ungu
        this.GunColor   = Color.FromArgb(255, 255, 165, 0);     // Oranye cerah
        this.TracksColor= Color.FromArgb(255, 100, 100, 100);   // Abu-abu gelap
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
            // Setting up gun
            this.ErrorCount = 0;
            if (this.GunDirection != this.Direction) {
                this.TurnGunLeft(this.CalcGunBearing(this.Direction));
            }
            this.AdjustRadarForGunTurn = true;

            // Scan musuh sebanyak 2 putaran
            this.TurnRadarLeft(360);
            if (this.TargetBot != null) {
                this.TurnRadarLeft(this.RadarBearingTo(this.TargetBot.X, this.TargetBot.Y));
            }
            // Mengambil musuh dengan speed terendah
            if (this.ScannedBots.Count > 0) {
                this.TargetBot = this.ScannedBots.Values.OrderBy(bot => bot.speed).ThenBy(bot => bot.energy).First();
                this.BotMode = Mode.LOCK_ON_TARGET;
                if (this.GunDirection != this.RadarDirection) {
                    this.TurnRadarLeft(this.CalcRadarBearing(this.GunDirection));
                }
                this.AdjustRadarForGunTurn = false;
            } else {
                this.RunCrazyMove(); // mencegah serangan gratis
            }
            this.Go();
        } catch (Exception ex) {
            Console.WriteLine($"Error di BruteScan: {ex.Message}");
        }
    }
    // Lock On Target Mode
    public void LockOnTarget() {
        try {
            // Cek eksistensi target
            if (this.TargetBot == null) { // target loss
                this.BotMode = Mode.BRUTE_SCAN;
            // Still on target
            } else {
                this.RunCrazyMove();
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
        this.ErrorCount++;
    }
    public override void OnBulletHit (BulletHitBotEvent e) {
        this.ErrorCount = 0;
    }


    // ----------------- Additional Method -----------------
    public void LockGun(double firepower) {
        try {

            // Evaluasi presisi posisi bot
            if (this.TargetBot != null) {
                if (this.ErrorCount > 3) {
                    this.TurnGunLeft(360);
                    this.ErrorCount = 0;
                }
                this.AdjustGunForBodyTurn = true;
                this.SetGunPrecision(firepower);
                this.AdjustGunForBodyTurn = false;
            }
        } catch (Exception ex) {
            Console.WriteLine($"Error di LockGun: {ex.Message}");
        }
    }
    public void RunCrazyMove() {
        try {
            // Ubah kecepatan secara dinamis untuk membuat pergerakan tidak terduga
            this.TargetSpeed = 2 + rand.NextDouble() * 6; // Speed antara 2 - 8
            // Perubahan arah tiba-tiba secara acak
            if ((int)(this.X + this.Y) % 3 == 0) {
                this.isMovingForward = !this.isMovingForward;
                this.SetTurnLeft(rand.Next(-90, 90));
            }
            // Jika keluar dari batas, putar kembali ke tengah
            if (this.X < this.ArenaWidth/8 || this.X > this.ArenaWidth/4 || this.Y < this.ArenaHeight/8 || this.Y > this.ArenaWidth/4) {
                this.SetTurnLeft(this.BearingTo(this.ArenaWidth/2, this.ArenaHeight/2));
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
    1. pada kondisi start, bot akan melakukan pemindaian 360 derajat sebanyak 2 putaran.
    2. selama pemindaian, bot mengkomparasi setiap speed dari bot yang dipindainya. bot musuh dengan speed rendah memiliki prioritas yang lebih tinggi.
    3. selama pemindaian, bot melakukan gerakan di area sekitar tengah dengan gerakan melingkar atau membentuk suatu pola tertentu agar tidak diam (silahkan pilih, jangan lupa untuk memainkan velocity dan arah gerak yang cukup "crazy"), dengan catatan, pola tersebut harus berada di sekitar tengah arena. pada mode ini, radar terpisah dari gun dan body agar dapat memindai dengan cepat.
    4. pemindaian sebanyak 2 putaran bertujuan untuk mengkomparasi perpindahan setiap bot musuh, bot musuh dengan perpindahan yang rendah memiliki prioritas lebih tinggi.
    5. setelah menentukan bot dengan prioritas tertinggi dari kedua aspek tersebut, bot memasuki lock on target mode.

    C. lock on target mode
    1. pada mode ini, bot hanya tertuju kepada satu bot yang sudah dipilih pada brute scan mode.
    2. body terpisah gun dan radar, gun dan radar saling menyatu dan selalu mengarah ke arah yang sama.
    3. bot akan selalu melakukan pergerakan dengan pola tertentu di sekitar area tengah (boleh menggunakan pola yang sama dengan brute scan mode, maupun berbeda), sementara gun dan radar selalu mengarah ke target tanpa peduli sepert apa posisi dan arah gerak bot kita saat ini. 
    4. bot akan menembak ke target apabila gunheat=0. pikirkan baik-baik "error value" akibat gerakan bot kita, maupun gerakan bot target. pikirkan pula firepower yang harus dibuat dengan mempertimbangkan jarak, kecepatan bullet, dan energy risk.
    5. bot berada dalam lock on target sampai target mati. setelah target mati, bot akan kembali ke brute scan mode untuk mencari target selanjutnya.
*/