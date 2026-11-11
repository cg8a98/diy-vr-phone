using System.IO;
using DiyVr.Room;
using NUnit.Framework;
using UnityEngine;

namespace DiyVr.Tests
{
    /// <summary>The maths of scan alignment (ticket 3.3): room and world poses, stick mapping, the saved file.</summary>
    public class ScanAlignMathTests
    {
        private const float Tol = 1e-4f;

        private static void AssertNear(Vector3 expected, Vector3 actual, float tol, string what)
        {
            Assert.Less(Vector3.Distance(expected, actual), tol, what + ": expected " + expected.ToString("F4") + " got " + actual.ToString("F4"));
        }

        // ---- room <-> world ----

        [Test]
        public void RoomToWorld_ThenWorldToRoom_GivesTheSamePose()
        {
            var room = new RoomPose(new Vector3(0.4f, 1.45f, 2.1f), 33f);
            var inRoom = new ScanPose(new Vector3(1f, -1.4f, 2f), -170f);

            ScanPose world = ScanAlignMath.RoomToWorld(room, inRoom);
            ScanPose back = ScanAlignMath.WorldToRoom(room, world);

            AssertNear(inRoom.Position, back.Position, Tol, "position");
            Assert.AreEqual(0f, Mathf.DeltaAngle(inRoom.YawDegrees, back.YawDegrees), 1e-3f, "yaw");
            Assert.AreEqual(0f, Mathf.DeltaAngle(-137f, world.YawDegrees), 1e-3f, "world yaw = room yaw + scan yaw");
        }

        [Test]
        public void RoomToWorld_IsTheSameAsHangingTheScanUnderTheRoomTransform()
        {
            var room = new RoomPose(new Vector3(-2f, 1.3f, 0.7f), 121f);
            var inRoom = new ScanPose(new Vector3(0.6f, -1.2f, -3f), 48f);
            var point = new Vector3(1.5f, 0.9f, -0.4f); // a spot in the scan

            var parent = new GameObject("room");
            var child = new GameObject("scan");
            try
            {
                parent.transform.SetPositionAndRotation(room.Origin, room.Rotation);
                child.transform.SetParent(parent.transform, false);
                child.transform.localPosition = inRoom.Position;
                child.transform.localRotation = inRoom.Rotation;

                ScanPose world = ScanAlignMath.RoomToWorld(room, inRoom);
                AssertNear(child.transform.TransformPoint(point), world.Position + world.Rotation * point, 1e-4f, "scan point");
            }
            finally
            {
                Object.DestroyImmediate(child);
                Object.DestroyImmediate(parent);
            }
        }

        [Test]
        public void NormalizeYaw_KeepsAnglesBetweenMinus180And180()
        {
            Assert.AreEqual(-170f, ScanAlignMath.NormalizeYaw(190f), 1e-3f);
            Assert.AreEqual(10f, ScanAlignMath.NormalizeYaw(370f), 1e-3f);
            Assert.AreEqual(-10f, ScanAlignMath.NormalizeYaw(-370f), 1e-3f);
        }

        // ---- sticks ----

        [Test]
        public void Stick_InsideTheDeadZone_IsZero()
        {
            Assert.AreEqual(Vector2.zero, ScanAlignMath.Stick(new Vector2(0.19f, 0f), ScanAlignMath.DeadZone));
            Assert.AreEqual(Vector2.zero, ScanAlignMath.Stick(new Vector2(0.12f, -0.12f), ScanAlignMath.DeadZone));
            Assert.AreEqual(0f, ScanAlignMath.Axis(-0.2f, ScanAlignMath.DeadZone));
        }

        [Test]
        public void Stick_OutsideTheDeadZone_IsSoftAndReachesOne()
        {
            Assert.AreEqual(0.25f, ScanAlignMath.Stick(new Vector2(0.6f, 0f), 0.2f).x, 1e-5f, "half way = quarter speed");
            Assert.AreEqual(1f, ScanAlignMath.Stick(new Vector2(0f, 1f), 0.2f).y, 1e-5f);
            Assert.AreEqual(1f, ScanAlignMath.Stick(new Vector2(2f, 0f), 0.2f).x, 1e-5f, "never more than full speed");
            Vector2 diagonal = ScanAlignMath.Stick(new Vector2(0.5f, -0.5f), 0.2f);
            Assert.Greater(diagonal.x, 0f);
            Assert.AreEqual(diagonal.x, -diagonal.y, 1e-6f, "direction kept");
            Assert.AreEqual(-0.25f, ScanAlignMath.Axis(-0.6f, 0.2f), 1e-5f);
            Assert.AreEqual(1f, ScanAlignMath.Axis(1f, 0.2f), 1e-5f);
        }

        [Test]
        public void Step_NoInputOrDeadZoneInput_DoesNotMoveTheScan()
        {
            var pose = new ScanPose(new Vector3(1f, 0.5f, -2f), 40f);
            var drift = new AlignInput { Move = new Vector2(0.1f, -0.15f), Turn = 0.19f, Lift = 0.09f };

            ScanPose after = ScanAlignMath.Step(pose, drift, 77f, new Vector3(3f, 1.6f, 3f), 0.5f);

            AssertNear(pose.Position, after.Position, 1e-7f, "position");
            Assert.AreEqual(pose.YawDegrees, after.YawDegrees, 1e-6f);
        }

        [Test]
        public void Step_LeftStick_SlidesAlongTheFloorRelativeToWhereYouLook()
        {
            var pose = new ScanPose(new Vector3(0f, 0.5f, 0f), 10f);
            var forward = new AlignInput { Move = new Vector2(0f, 1f) };
            var right = new AlignInput { Move = new Vector2(1f, 0f) };

            // Looking along +X (yaw 90): "away from me" is +X, "to my right" is -Z.
            ScanPose a = ScanAlignMath.Step(pose, forward, 90f, Vector3.zero, 2f);
            AssertNear(new Vector3(ScanAlignMath.MoveSpeed * 2f, 0.5f, 0f), a.Position, Tol, "forward");
            ScanPose b = ScanAlignMath.Step(pose, right, 90f, Vector3.zero, 2f);
            AssertNear(new Vector3(0f, 0.5f, -ScanAlignMath.MoveSpeed * 2f), b.Position, Tol, "right");
            Assert.AreEqual(10f, a.YawDegrees, 1e-5f, "sliding does not turn");
        }

        [Test]
        public void Step_FineMode_IsTenTimesSlower()
        {
            var pose = new ScanPose(Vector3.zero, 0f);
            var coarse = new AlignInput { Move = new Vector2(0f, 1f), Turn = 1f, Lift = 1f };
            var fine = coarse;
            fine.Fine = true;

            ScanPose c = ScanAlignMath.Step(pose, coarse, 0f, Vector3.zero, 1f);
            ScanPose f = ScanAlignMath.Step(pose, fine, 0f, Vector3.zero, 1f);

            Assert.AreEqual(ScanAlignMath.MoveSpeed, c.Position.z, Tol);
            Assert.AreEqual(ScanAlignMath.MoveSpeed * ScanAlignMath.FineFactor, f.Position.z, Tol);
            Assert.AreEqual(ScanAlignMath.TurnSpeed, c.YawDegrees, 1e-3f);
            Assert.AreEqual(ScanAlignMath.TurnSpeed * ScanAlignMath.FineFactor, f.YawDegrees, 1e-3f);
            Assert.AreEqual(ScanAlignMath.LiftSpeed, c.Position.y, Tol);
            Assert.AreEqual(ScanAlignMath.LiftSpeed * ScanAlignMath.FineFactor, f.Position.y, Tol);
            Assert.AreEqual(0.1f, ScanAlignMath.FineFactor, 1e-6f);
        }

        [Test]
        public void Step_RightStick_TurnsTheScanAroundWhereYouStand()
        {
            var pivot = new Vector3(2f, 1.6f, 1f);
            var pose = new ScanPose(pivot + new Vector3(1f, -1.1f, 0f), 15f);
            var turn = new AlignInput { Turn = 1f };

            // 3 s at 30 degrees per second = a quarter turn clockwise seen from above: +X swings round to -Z.
            ScanPose after = ScanAlignMath.Step(pose, turn, 0f, pivot, 90f / ScanAlignMath.TurnSpeed);

            AssertNear(pivot + new Vector3(0f, -1.1f, -1f), after.Position, 1e-4f, "position");
            Assert.AreEqual(105f, after.YawDegrees, 1e-3f);

            // A spot of the scan that is right at the pivot stays where it is.
            Vector3 local = Quaternion.Inverse(pose.Rotation) * (pivot - pose.Position);
            AssertNear(pivot, after.Position + after.Rotation * local, 1e-4f, "the spot under you");
        }

        [Test]
        public void Step_Triggers_LiftAndLower()
        {
            var pose = new ScanPose(new Vector3(1f, 0f, 1f), 0f);
            ScanPose up = ScanAlignMath.Step(pose, new AlignInput { Lift = 1f }, 0f, Vector3.zero, 1f);
            ScanPose down = ScanAlignMath.Step(pose, new AlignInput { Lift = -1f }, 0f, Vector3.zero, 1f);
            AssertNear(new Vector3(1f, ScanAlignMath.LiftSpeed, 1f), up.Position, Tol, "up");
            AssertNear(new Vector3(1f, -ScanAlignMath.LiftSpeed, 1f), down.Position, Tol, "down");
        }

        [Test]
        public void LookYaw_UsesTheTopOfTheHead_WhenLookingStraightDownOrUp()
        {
            Assert.AreEqual(0f, ScanAlignMath.LookYaw(Vector3.forward, Vector3.up), 1e-3f);
            Assert.AreEqual(90f, ScanAlignMath.LookYaw(Vector3.right, Vector3.up), 1e-3f);
            Assert.AreEqual(90f, ScanAlignMath.LookYaw(Vector3.down, Vector3.right), 1e-3f, "looking down");
            Assert.AreEqual(90f, ScanAlignMath.LookYaw(Vector3.up, Vector3.left), 1e-3f, "looking up");
            Assert.AreEqual(0f, ScanAlignMath.LookYaw(Vector3.down, Vector3.up), 1e-3f, "no direction at all: no crash");
        }

        [Test]
        public void StartingGuess_PutsTheScanInFrontOfYou_OnTheFloor()
        {
            var bounds = new Bounds(new Vector3(1f, 1.45f, 1f), new Vector3(4f, 2.5f, 6f)); // lowest point y = 0.2
            var head = new Vector3(2f, 1.6f, 3f);

            ScanPose guess = ScanAlignMath.StartingGuess(bounds, head, 90f, 0.1f);

            AssertNear(new Vector3(2f + ScanAlignMath.GuessDistance - 1f, -0.1f, 2f), guess.Position, Tol, "guess");
            Assert.AreEqual(0.1f, guess.Position.y + bounds.min.y, Tol, "lowest point on the floor");
            Assert.AreEqual(0f, guess.YawDegrees);
        }

        [Test]
        public void StatusText_SaysSavedLockedAndResidual()
        {
            Assert.AreEqual("Align: saved, frame locked yes, marker residual 0.4 cm", ScanAlignMath.StatusText(true, true, 0.004f));
            Assert.AreEqual("Align: not saved, frame locked no, marker residual -", ScanAlignMath.StatusText(false, false, 0.5f));
        }

        // ---- buttons ----

        [Test]
        public void Hold_FiresOnceAfterOneSecond_AndNeedsARelease()
        {
            var hold = new AlignHoldLogic();
            Assert.IsFalse(hold.Update(true, 0.5f));
            Assert.IsFalse(hold.Update(true, 0.49f));
            Assert.IsTrue(hold.Update(true, 0.02f), "one second reached");
            Assert.IsFalse(hold.Update(true, 5f), "only once per hold");
            Assert.IsFalse(hold.Update(false, 0.1f));
            Assert.IsFalse(hold.Update(true, 0.3f), "a short press does nothing");
            Assert.IsFalse(hold.Update(false, 0.1f));
            Assert.IsFalse(hold.Update(true, 0.8f), "time starts again after a release");
            Assert.IsTrue(hold.Update(true, 0.3f));
        }

        [Test]
        public void Buttons_CountOnlyNewPresses_AfterARelease()
        {
            var buttons = new AlignButtons();
            bool save, cancel;

            buttons.Update(true, false, out save, out cancel);
            Assert.IsFalse(save, "A was already down when align mode opened");
            buttons.Update(false, false, out save, out cancel);
            buttons.Update(true, false, out save, out cancel);
            Assert.IsTrue(save);
            Assert.IsFalse(cancel);
            buttons.Update(true, false, out save, out cancel);
            Assert.IsFalse(save, "holding does not repeat");

            buttons.Update(false, true, out save, out cancel);
            Assert.IsTrue(cancel);
            Assert.IsFalse(save);

            buttons.Block();
            buttons.Update(true, true, out save, out cancel);
            Assert.IsFalse(save || cancel, "blocked until released");
            buttons.Update(false, false, out save, out cancel);
            buttons.Update(true, true, out save, out cancel);
            Assert.IsTrue(save, "both at once: save wins");
            Assert.IsFalse(cancel);
        }

        // ---- saved file ----

        [Test]
        public void Store_SaveAndLoad_RoundTrip_PerScan()
        {
            string path = Path.Combine(Application.temporaryCachePath, "scan-align-" + System.Guid.NewGuid().ToString("N") + ".json");
            try
            {
                var store = new ScanAlignStore(path);
                ScanPose pose;
                Assert.IsFalse(store.TryGet("living-room", out pose), "nothing saved yet");
                Assert.IsFalse(File.Exists(path));

                string error;
                Assert.IsTrue(store.Set("living-room", new ScanPose(new Vector3(1.25f, -1.375f, -0.5f), 123.5f), out error), error);
                Assert.IsTrue(store.Set("office", new ScanPose(new Vector3(-3f, 0f, 2f), -45f), out error), error);
                Assert.IsTrue(store.Set("living-room", new ScanPose(new Vector3(1.5f, -1.4f, -0.25f), 124f), out error), "replace");
                Assert.AreEqual(2, store.Count);

                var again = new ScanAlignStore(path);
                Assert.AreEqual(2, again.Count);
                Assert.IsTrue(again.TryGet("living-room", out pose));
                AssertNear(new Vector3(1.5f, -1.4f, -0.25f), pose.Position, 1e-6f, "living room");
                Assert.AreEqual(124f, pose.YawDegrees, 1e-5f);
                Assert.IsTrue(again.TryGet("office", out pose));
                AssertNear(new Vector3(-3f, 0f, 2f), pose.Position, 1e-6f, "office");
                Assert.AreEqual(-45f, pose.YawDegrees, 1e-5f);
                Assert.IsFalse(again.TryGet("kitchen", out pose));
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [Test]
        public void Store_DamagedFile_MeansNothingSaved_AndCanBeSavedOver()
        {
            string path = Path.Combine(Application.temporaryCachePath, "scan-align-" + System.Guid.NewGuid().ToString("N") + ".json");
            try
            {
                File.WriteAllText(path, "{ this is not json");
                var store = new ScanAlignStore(path);
                ScanPose pose;
                Assert.AreEqual(0, store.Count);
                Assert.IsFalse(store.TryGet("living-room", out pose));

                string error;
                Assert.IsTrue(store.Set("living-room", new ScanPose(Vector3.one, 5f), out error), error);
                Assert.IsTrue(new ScanAlignStore(path).TryGet("living-room", out pose));
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [Test]
        public void SafeKey_KeepsSimpleNames_AndReplacesTheRest()
        {
            Assert.AreEqual("living-room_2.v1", ScanAlignStore.SafeKey("Living-Room_2.v1"));
            Assert.AreEqual("my_room__1_", ScanAlignStore.SafeKey("my room (1)"));
            Assert.AreEqual("scan", ScanAlignStore.SafeKey(null));
            Assert.AreEqual("scan", ScanAlignStore.SafeKey(""));
            Assert.AreEqual(80, ScanAlignStore.SafeKey(new string('a', 300)).Length);
        }
    }
}
