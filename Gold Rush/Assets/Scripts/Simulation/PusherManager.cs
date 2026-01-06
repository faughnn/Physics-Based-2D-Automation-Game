using UnityEngine;
using System.Collections.Generic;
using GoldRush.Core;
using GoldRush.Infrastructure;

namespace GoldRush.Simulation
{
    public enum PusherPhase
    {
        Retracted,      // Plate at start position, pausing
        Extending,      // Moving toward exit
        Extended,       // Paused at far position
        Retracting      // Moving back to start
    }

    public enum PushDirection
    {
        Right,
        Left,
        Up,
        Down
    }

    public class PusherManager
    {
        private static PusherManager _instance;
        public static PusherManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new PusherManager();
                return _instance;
            }
        }

        private List<Pusher> pushers = new List<Pusher>();
        private int frameCounter = 0;

        // Current global phase state
        public PusherPhase CurrentPhase { get; private set; } = PusherPhase.Retracted;

        // Target extension: 0 = fully retracted, 1 = fully extended
        // This is the TARGET - individual pushers may be behind if blocked
        public float TargetExtension { get; private set; } = 0f;

        public void RegisterPusher(Pusher pusher)
        {
            if (!pushers.Contains(pusher))
                pushers.Add(pusher);
        }

        public void UnregisterPusher(Pusher pusher)
        {
            pushers.Remove(pusher);
        }

        // Called every frame from SimulationGrid.Update()
        public void Update(SimulationGrid grid)
        {
            if (pushers.Count == 0)
                return;

            frameCounter++;
            UpdatePhaseStateMachine();

            // All pushers process together with the same target
            foreach (var pusher in pushers)
            {
                pusher.ProcessPhysics(grid, TargetExtension);
            }
        }

        private void UpdatePhaseStateMachine()
        {
            switch (CurrentPhase)
            {
                case PusherPhase.Retracted:
                    // Pausing at retracted position
                    if (frameCounter >= GameSettings.PusherPauseFrames)
                    {
                        frameCounter = 0;
                        CurrentPhase = PusherPhase.Extending;
                    }
                    TargetExtension = 0f;
                    break;

                case PusherPhase.Extending:
                    // Moving from 0 to 1
                    TargetExtension = (float)frameCounter / GameSettings.PusherExtendFrames;
                    if (frameCounter >= GameSettings.PusherExtendFrames)
                    {
                        frameCounter = 0;
                        CurrentPhase = PusherPhase.Extended;
                        TargetExtension = 1f;
                    }
                    break;

                case PusherPhase.Extended:
                    // Pausing at extended position
                    if (frameCounter >= GameSettings.PusherPauseFrames)
                    {
                        frameCounter = 0;
                        CurrentPhase = PusherPhase.Retracting;
                    }
                    TargetExtension = 1f;
                    break;

                case PusherPhase.Retracting:
                    // Moving from 1 to 0
                    TargetExtension = 1f - (float)frameCounter / GameSettings.PusherRetractFrames;
                    if (frameCounter >= GameSettings.PusherRetractFrames)
                    {
                        frameCounter = 0;
                        CurrentPhase = PusherPhase.Retracted;
                        TargetExtension = 0f;
                    }
                    break;
            }
        }

        public void Clear()
        {
            pushers.Clear();
            frameCounter = 0;
            CurrentPhase = PusherPhase.Retracted;
            TargetExtension = 0f;
        }

        public int PusherCount => pushers.Count;
    }
}
