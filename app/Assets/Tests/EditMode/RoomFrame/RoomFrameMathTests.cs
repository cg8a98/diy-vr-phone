using System.IO;
using DiyVr.Room;
using NUnit.Framework;
using UnityEngine;

namespace DiyVr.Tests
{
    /// <summary>The room-frame maths (ticket 3.2): two points to a frame, averaging, outliers, saved file.</summary>
    public class RoomFrameMathTests
    {
        private const float Mm = 0.001f;

        private static void AssertNear(Vector3 expected, Vector3 actual, float tolerance, string what)
        {
            Assert.LessOrEqual(Vector3.Distance(expected, actual), tolerance, what + ": expected " + expected.ToString("F4") + " got " + actual.ToString("F4"));
        }

        private static Vector3 Noise(System.Random rnd, float size)
        {
            return new Vector3((float)(rnd.NextDouble() * 2 - 1), (float)(rnd.NextDouble() * 2 - 1), (float)(rnd.NextDouble() * 2 - 1)) * size;
        }

        // ---- two points -> frame ----

        [Test]
        public void FromMarkers_OriginIsMarkerA_ZPointsTowardsMarkerB()
        {
            var a = new Vector3(1f, 1.4f, 2f);
            RoomPose pose;
            Assert.IsTrue(RoomFrameMath.TryFromMarkers(a, new Vector3(1f, 1.4f, 4f), out pose));
            AssertNear(a, pose.Origin, 1e-5f, "origin");
            Assert.AreEqual(0f, pose.YawDegrees, 1e-3f);
            AssertNear(a + Vector3.forward, pose.RoomToWorld(Vector3.forward), 1e-5f, "room +Z");

            Assert.IsTrue(RoomFrameMath.TryFromMarkers(a, new Vector3(3f, 1.4f, 2f), out pose));
            Assert.AreEqual(90f, pose.YawDegrees, 1e-3f);
            AssertNear(a + Vector3.right, pose.RoomToWorld(Vector3.forward), 1e-5f, "room +Z after a quarter turn");
            AssertNear(a + Vector3.up, pose.RoomToWorld(Vector3.up), 1e-5f, "room +Y stays up");
        }

        [Test]
        public void FromMarkers_HeightDifferenceDoesNotTiltOrTurnTheRoom()
        {
            var a = new Vector3(0.2f, 1.3f, -0.5f);
            RoomPose level, higher;
            Assert.IsTrue(RoomFrameMath.TryFromMarkers(a, new Vector3(1.2f, 1.3f, 0.7f), out level));
            Assert.IsTrue(RoomFrameMath.TryFromMarkers(a, new Vector3(1.2f, 1.75f, 0.7f), out higher));
            Assert.AreEqual(level.YawDegrees, higher.YawDegrees, 1e-4f);
            AssertNear(Vector3.up, higher.Rotation * Vector3.up, 1e-5f, "up");
        }

        [Test]
        public void FromMarkers_MarkerBInRoom_IsStraightAhead()
        {
            var a = new Vector3(-1f, 1.2f, 3f);
            var b = new Vector3(0.5f, 1.5f, 2f);
            RoomPose pose;
            Assert.IsTrue(RoomFrameMath.TryFromMarkers(a, b, out pose));
            Vector3 bRoom = pose.WorldToRoom(b);
            float floorDistance = new Vector2(b.x - a.x, b.z - a.z).magnitude;
            AssertNear(new Vector3(0f, 0.3f, floorDistance), bRoom, 1e-4f, "marker B in room");
        }

        [Test]
        public void FromMarkers_MarkersAboveEachOther_Fails()
        {
            RoomPose pose;
            Assert.IsFalse(RoomFrameMath.TryFromMarkers(new Vector3(0f, 1f, 0f), new Vector3(0.1f, 2f, 0.1f), out pose));
        }

        // ---- room <-> world ----

        [Test]
        public void RoomWorld_RoundTrip_PointsAndRotations()
        {
            var rnd = new System.Random(7);
            for (int i = 0; i < 50; i++)
            {
                var pose = new RoomPose(Noise(rnd, 5f), (float)(rnd.NextDouble() * 360.0 - 180.0));
                Vector3 p = Noise(rnd, 4f);
                AssertNear(p, pose.WorldToRoom(pose.RoomToWorld(p)), 1e-4f, "point round trip");
                AssertNear(p, pose.RoomToWorld(pose.WorldToRoom(p)), 1e-4f, "point round trip back");
                Quaternion q = Quaternion.Euler(Noise(rnd, 180f));
                Assert.Less(Quaternion.Angle(q, pose.WorldToRoomRotation(pose.RoomToWorldRotation(q))), 0.01f);
            }
        }

        [Test]
        public void RoomCoordinates_AreTheSame_WhateverTheWorldOriginIs()
        {
            // ARCore starts every session with a different world origin and heading. The room coordinates of a real
            // point must not care.
            var a = new Vector3(0f, 1.4f, 0f);
            var b = new Vector3(1.6f, 1.45f, 0.3f);
            var point = new Vector3(0.7f, 0.9f, 1.9f);
            RoomPose first;
            Assert.IsTrue(RoomFrameMath.TryFromMarkers(a, b, out first));
            Vector3 expected = first.WorldToRoom(point);

            var rnd = new System.Random(11);
            for (int i = 0; i < 20; i++)
            {
                Quaternion turn = Quaternion.Euler(0f, (float)(rnd.NextDouble() * 360.0), 0f);
                Vector3 shift = Noise(rnd, 3f);
                RoomPose pose;
                Assert.IsTrue(RoomFrameMath.TryFromMarkers(turn * a + shift, turn * b + shift, out pose));
                AssertNear(expected, pose.WorldToRoom(turn * point + shift), 1e-4f, "session " + i);
            }
        }

        // ---- yaw stability ----

        [Test]
        public void Yaw_WithFiveMillimetresOfNoise_StaysWithinHalfADegree()
        {
            var a = new Vector3(0f, 1.4f, 0f);
            var b = new Vector3(1.5f, 1.4f, 0f);
            RoomPose truth;
            RoomFrameMath.TryFromMarkers(a, b, out truth);
            var rnd = new System.Random(3);
            for (int i = 0; i < 200; i++)
            {
                RoomPose noisy;
                Assert.IsTrue(RoomFrameMath.TryFromMarkers(a + Noise(rnd, 5 * Mm), b + Noise(rnd, 5 * Mm), out noisy));
                Assert.Less(RoomFrameMath.AngleDifference(truth.YawDegrees, noisy.YawDegrees), 0.5f);
            }
        }

        [Test]
        public void Yaw_AveragedOverTheWindow_IsFiveTimesSteadier()
        {
            var a = new Vector3(0f, 1.4f, 0f);
            var b = new Vector3(1.5f, 1.4f, 0f);
            RoomPose truth;
            RoomFrameMath.TryFromMarkers(a, b, out truth);
            var rnd = new System.Random(5);
            for (int run = 0; run < 20; run++)
            {
                var solver = new RoomFrameSolver();
                for (int i = 0; i < solver.WindowSize; i++)
                {
                    solver.AddSample(0, a + Noise(rnd, 5 * Mm), i * 0.033f);
                    solver.AddSample(1, b + Noise(rnd, 5 * Mm), i * 0.033f);
                }
                RoomSolution s;
                Assert.IsTrue(solver.TrySolve(out s));
                Assert.Less(RoomFrameMath.AngleDifference(truth.YawDegrees, s.Pose.YawDegrees), 0.1f);
                AssertNear(a, s.Pose.Origin, 3 * Mm, "origin");
                Assert.Less(s.Spread, 7 * Mm);
            }
        }

        // ---- averaging and outliers ----

        [Test]
        public void Averager_NeedsAFullWindow()
        {
            var avg = new MarkerAverager(10);
            Vector3 mean;
            float spread;
            for (int i = 0; i < 9; i++)
            {
                Assert.IsTrue(avg.Add(new Vector3(i * Mm, 0f, 0f), i));
                Assert.IsFalse(avg.TryGetEstimate(out mean, out spread));
            }
            avg.Add(new Vector3(9 * Mm, 0f, 0f), 9f);
            Assert.IsTrue(avg.IsFull);
            Assert.IsTrue(avg.TryGetEstimate(out mean, out spread));
            AssertNear(new Vector3(4.5f * Mm, 0f, 0f), mean, 1e-5f, "mean");
            Assert.AreEqual(9f, avg.LastTime);
        }

        [Test]
        public void Averager_IgnoresARepeatOfTheSameCameraFrame()
        {
            var avg = new MarkerAverager(5);
            var p = new Vector3(1f, 2f, 3f);
            Assert.IsTrue(avg.Add(p, 0f));
            Assert.IsFalse(avg.Add(p, 0.01f));
            Assert.AreEqual(1, avg.Count);
            Assert.AreEqual(0f, avg.LastTime);
        }

        [Test]
        public void Averager_ThrowsOutOutliers()
        {
            var truth = new Vector3(2f, 1.5f, -1f);
            var rnd = new System.Random(9);
            var avg = new MarkerAverager(30);
            for (int i = 0; i < 30; i++)
            {
                // Every sixth sample is a wild detection 20 to 60 cm away.
                Vector3 p = i % 6 == 0 ? truth + new Vector3(0.2f + 0.08f * i / 6, -0.3f, 0.1f) : truth + Noise(rnd, 2 * Mm);
                avg.Add(p, i);
            }
            Vector3 mean;
            float spread;
            Assert.IsTrue(avg.TryGetEstimate(out mean, out spread));
            AssertNear(truth, mean, 1.5f * Mm, "mean without the outliers");
            Assert.Less(spread, 4 * Mm);
        }

        [Test]
        public void Averager_KeepsOnlyTheNewestWindow()
        {
            var avg = new MarkerAverager(10);
            var oldSpot = new Vector3(0f, 0f, 0f);
            var newSpot = new Vector3(1f, 0f, 0f);
            for (int i = 0; i < 10; i++) avg.Add(oldSpot + new Vector3(0f, i * 1e-4f, 0f), i);
            for (int i = 0; i < 10; i++) avg.Add(newSpot + new Vector3(0f, i * 1e-4f, 0f), 10 + i);
            Vector3 mean;
            float spread;
            Assert.IsTrue(avg.TryGetEstimate(out mean, out spread));
            AssertNear(newSpot, mean, Mm, "mean after the marker pose jumped");
        }

        [Test]
        public void Averager_SamplesThatDoNotAgree_GiveNoEstimate()
        {
            // Three groups a metre apart: the marker pose was jumping about, so there is nothing to trust.
            var avg = new MarkerAverager(9);
            for (int i = 0; i < 9; i++) avg.Add(new Vector3(i % 3, i * 1e-4f, 0f), i);
            Vector3 mean;
            float spread;
            Assert.IsFalse(avg.TryGetEstimate(out mean, out spread));
        }

        // ---- solver ----

        [Test]
        public void Solver_NeedsBothMarkers_SeenCloseInTime()
        {
            var a = new Vector3(0f, 1.4f, 0f);
            var b = new Vector3(0f, 1.6f, 1.2f);
            var solver = new RoomFrameSolver(10);
            RoomSolution s;
            for (int i = 0; i < 10; i++) solver.AddSample(0, a + new Vector3(0f, i * 1e-5f, 0f), i * 0.03f);
            Assert.IsFalse(solver.TrySolve(out s), "only marker A seen");
            Assert.AreEqual(10, solver.SampleCount(0));
            Assert.AreEqual(0, solver.SampleCount(1));

            // Marker B seen a minute later: too much drift could sit between the two.
            for (int i = 0; i < 10; i++) solver.AddSample(1, b + new Vector3(0f, i * 1e-5f, 0f), 60f + i * 0.03f);
            Assert.IsFalse(solver.TrySolve(out s), "markers seen too far apart in time");

            // Marker A looked at again: now it works.
            for (int i = 0; i < 10; i++) solver.AddSample(0, a + new Vector3(0f, i * 1e-5f + 1e-6f, 0f), 62f + i * 0.03f);
            Assert.IsTrue(solver.TrySolve(out s));
            AssertNear(new Vector3(0f, 0.2f, 1.2f), s.MarkerBInRoom, Mm, "marker B in room");

            Assert.IsFalse(solver.AddSample(2, a, 0f), "there is no third marker");
            solver.Clear();
            Assert.IsFalse(solver.TrySolve(out s));
        }

        // ---- saved file ----

        [Test]
        public void Data_JsonRoundTrip_KeepsEverything()
        {
            var data = new RoomFrameData
            {
                calibrated = true,
                markerWidth = RoomMarkers.WidthMeters,
                markerBInRoom = new Vector3(0f, 0.123f, 1.456f),
                hasFloor = true,
                floorY = -1.35f,
                hasTestObject = true,
                testObject = new Vector3(0.5f, -0.25f, 2.75f),
                savedUtc = "2026-11-10T12:00:00Z",
            };
            RoomFrameData back = RoomFrameData.FromJson(data.ToJson());
            Assert.IsNotNull(back);
            Assert.AreEqual(RoomFrameData.CurrentVersion, back.version);
            Assert.IsTrue(back.calibrated);
            Assert.AreEqual(data.markerWidth, back.markerWidth, 1e-6f);
            AssertNear(data.markerBInRoom, back.markerBInRoom, 1e-6f, "marker B");
            Assert.IsTrue(back.hasFloor);
            Assert.AreEqual(data.floorY, back.floorY, 1e-6f);
            Assert.IsTrue(back.hasTestObject);
            AssertNear(data.testObject, back.testObject, 1e-6f, "test object");
            Assert.AreEqual(data.savedUtc, back.savedUtc);
        }

        [Test]
        public void Data_BrokenOrNewerJson_IsRejected()
        {
            Assert.IsNull(RoomFrameData.FromJson(null));
            Assert.IsNull(RoomFrameData.FromJson(""));
            Assert.IsNull(RoomFrameData.FromJson("not json at all {"));
            Assert.IsNull(RoomFrameData.FromJson("{\"version\":99,\"calibrated\":true}"));
            Assert.IsNull(RoomFrameData.FromJson("{\"version\":0}"));
        }

        [Test]
        public void Data_SaveThenLoad_FromAFile_AndMissingFileMeansNotCalibrated()
        {
            string path = Path.Combine(Application.temporaryCachePath, "roomframe-test-" + System.Guid.NewGuid().ToString("N") + ".json");
            try
            {
                Assert.IsFalse(RoomFrameData.Load(path).calibrated, "no file yet");

                var data = new RoomFrameData { calibrated = true, markerBInRoom = new Vector3(0f, 0.1f, 1.5f) };
                Assert.IsTrue(data.Save(path));
                Assert.IsTrue(File.Exists(path));
                Assert.IsFalse(File.Exists(path + ".tmp"), "temporary file left behind");
                Assert.IsNotEmpty(data.savedUtc);

                // Saving again over an existing file works too.
                data.hasTestObject = true;
                Assert.IsTrue(data.Save(path));

                RoomFrameData back = RoomFrameData.Load(path);
                Assert.IsTrue(back.calibrated);
                Assert.IsTrue(back.hasTestObject);
                AssertNear(data.markerBInRoom, back.markerBInRoom, 1e-6f, "marker B");

                File.WriteAllText(path, "garbage");
                Assert.IsFalse(RoomFrameData.Load(path).calibrated, "a broken file is treated as no file");
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }
    }
}
