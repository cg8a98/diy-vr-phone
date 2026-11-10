using System.Globalization;
using UnityEngine;

namespace DiyVr.Room
{
    /// <summary>
    /// Where the scan sits: a position and a turn around the vertical axis. No tilt: ARCore knows which way is
    /// down and Scaniverse exports the scan upright. Used both "in the room" (what is saved) and "in the world".
    /// </summary>
    public struct ScanPose
    {
        public Vector3 Position;
        /// <summary>Turn around the vertical axis, degrees.</summary>
        public float YawDegrees;

        public ScanPose(Vector3 position, float yawDegrees)
        {
            Position = position;
            YawDegrees = yawDegrees;
        }

        public Quaternion Rotation { get { return Quaternion.Euler(0f, YawDegrees, 0f); } }
    }

    /// <summary>The controller, as align mode reads it. All values are raw (-1..1).</summary>
    public struct AlignInput
    {
        /// <summary>Left stick: x = right, y = away from you.</summary>
        public Vector2 Move;
        /// <summary>Right stick X: positive turns the scan clockwise seen from above.</summary>
        public float Turn;
        /// <summary>Right trigger minus left trigger: positive lifts the scan.</summary>
        public float Lift;
        /// <summary>A shoulder button is held: ten times slower.</summary>
        public bool Fine;
    }

    /// <summary>Pure maths for scan alignment, so it can be unit tested without a phone.</summary>
    public static class ScanAlignMath
    {
        /// <summary>Stick movement below this is ignored (worn sticks never rest exactly at zero).</summary>
        public const float DeadZone = 0.2f;
        public const float TriggerDeadZone = 0.1f;
        /// <summary>Metres per second at full stick.</summary>
        public const float MoveSpeed = 0.5f;
        /// <summary>Degrees per second at full stick.</summary>
        public const float TurnSpeed = 30f;
        /// <summary>Metres per second at full trigger.</summary>
        public const float LiftSpeed = 0.2f;
        /// <summary>Fine mode multiplies every speed by this.</summary>
        public const float FineFactor = 0.1f;
        /// <summary>The starting guess puts the middle of the scan this far in front of you.</summary>
        public const float GuessDistance = 1.5f;
        /// <summary>Used for the floor when it is not known yet: eyes about this high above it.</summary>
        public const float GuessEyeHeight = 1.5f;

        /// <summary>Scan pose in the world from the room frame and the saved scan-in-room pose.</summary>
        public static ScanPose RoomToWorld(RoomPose room, ScanPose inRoom)
        {
            return new ScanPose(room.RoomToWorld(inRoom.Position), NormalizeYaw(room.YawDegrees + inRoom.YawDegrees));
        }

        /// <summary>Scan-in-room pose from where the scan is in the world now.</summary>
        public static ScanPose WorldToRoom(RoomPose room, ScanPose inWorld)
        {
            return new ScanPose(room.WorldToRoom(inWorld.Position), NormalizeYaw(inWorld.YawDegrees - room.YawDegrees));
        }

        /// <summary>Angle in -180..180 degrees.</summary>
        public static float NormalizeYaw(float degrees)
        {
            return Mathf.DeltaAngle(0f, degrees);
        }

        /// <summary>One axis: nothing inside the dead zone, then a soft (squared) curve up to 1 at full push.</summary>
        public static float Axis(float value, float deadZone)
        {
            float a = Mathf.Abs(value);
            if (a <= deadZone) return 0f;
            float t = (Mathf.Min(a, 1f) - deadZone) / (1f - deadZone);
            return Mathf.Sign(value) * t * t;
        }

        /// <summary>A stick: round dead zone, direction kept, soft (squared) curve up to length 1.</summary>
        public static Vector2 Stick(Vector2 value, float deadZone)
        {
            float m = value.magnitude;
            if (m <= deadZone) return Vector2.zero;
            float t = (Mathf.Min(m, 1f) - deadZone) / (1f - deadZone);
            return value / m * (t * t);
        }

        /// <summary>
        /// Which way "forward" is along the floor, degrees, from where the head looks. Looking (almost) straight
        /// down or up, the top of the head says which way is forward instead.
        /// </summary>
        public static float LookYaw(Vector3 forward, Vector3 up)
        {
            Vector3 flat = new Vector3(forward.x, 0f, forward.z);
            if (flat.sqrMagnitude < 0.04f)
            {
                flat = forward.y < 0f ? up : -up;
                flat.y = 0f;
                if (flat.sqrMagnitude < 1e-6f) return 0f;
            }
            return Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg;
        }

        /// <summary>
        /// One frame of align mode. The left stick slides the scan along the floor relative to where you look, the
        /// triggers lift or lower it, the right stick turns it around the spot where you stand (<paramref name="pivot"/>),
        /// so the wall in front of you does not swing away while you turn.
        /// </summary>
        public static ScanPose Step(ScanPose world, AlignInput input, float lookYawDegrees, Vector3 pivot, float deltaTime)
        {
            Vector2 move = Stick(input.Move, DeadZone);
            float turn = Axis(input.Turn, DeadZone);
            float lift = Axis(input.Lift, TriggerDeadZone);
            float scale = (input.Fine ? FineFactor : 1f) * Mathf.Max(0f, deltaTime);

            Vector3 position = world.Position;
            float yawStep = turn * TurnSpeed * scale;
            if (yawStep != 0f)
            {
                Vector3 offset = position - pivot;
                float y = offset.y;
                offset = Quaternion.Euler(0f, yawStep, 0f) * offset;
                offset.y = y;
                position = pivot + offset;
            }

            Vector3 slide = Quaternion.Euler(0f, lookYawDegrees, 0f) * new Vector3(move.x, 0f, move.y) * (MoveSpeed * scale);
            slide.y = lift * LiftSpeed * scale;
            return new ScanPose(position + slide, NormalizeYaw(world.YawDegrees + yawStep));
        }

        /// <summary>
        /// Where to put a scan that was never aligned: its middle <see cref="GuessDistance"/> in front of you, its
        /// lowest point on the floor, not turned. <paramref name="scanBounds"/> is the scan's box in its own coordinates.
        /// </summary>
        public static ScanPose StartingGuess(Bounds scanBounds, Vector3 headPosition, float lookYawDegrees, float floorY)
        {
            Vector3 target = headPosition + Quaternion.Euler(0f, lookYawDegrees, 0f) * new Vector3(0f, 0f, GuessDistance);
            return new ScanPose(
                new Vector3(target.x - scanBounds.center.x, floorY - scanBounds.min.y, target.z - scanBounds.center.z),
                0f);
        }

        /// <summary>The status line: "Align: saved, frame locked yes, marker residual 0.4 cm".</summary>
        public static string StatusText(bool saved, bool frameLocked, float residualMeters)
        {
            string residual = frameLocked
                ? (residualMeters * 100f).ToString("0.0", CultureInfo.InvariantCulture) + " cm"
                : "-";
            return "Align: " + (saved ? "saved" : "not saved")
                + ", frame locked " + (frameLocked ? "yes" : "no")
                + ", marker residual " + residual;
        }
    }

    /// <summary>"Hold the button for a second": fires once per hold.</summary>
    public sealed class AlignHoldLogic
    {
        public const float HoldSeconds = 1f;

        private float _held;
        private bool _fired;

        /// <summary>Call every frame. True on the one frame the hold reaches <see cref="HoldSeconds"/>.</summary>
        public bool Update(bool pressed, float deltaTime)
        {
            if (!pressed)
            {
                Reset();
                return false;
            }
            if (_fired) return false;
            _held += Mathf.Max(0f, deltaTime);
            if (_held < HoldSeconds) return false;
            _fired = true;
            return true;
        }

        public void Reset()
        {
            _held = 0f;
            _fired = false;
        }
    }

    /// <summary>
    /// A (save) and B (cancel) as presses, not holds. After <see cref="Block"/> a button must be let go before it
    /// counts again, so a button that was already down when align mode opened does nothing.
    /// </summary>
    public sealed class AlignButtons
    {
        private bool _a = true;
        private bool _b = true;

        public void Block()
        {
            _a = true;
            _b = true;
        }

        public void Update(bool a, bool b, out bool savePressed, out bool cancelPressed)
        {
            savePressed = a && !_a;
            cancelPressed = b && !_b && !savePressed;
            _a = a;
            _b = b;
        }
    }
}
