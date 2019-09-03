﻿using System;
using System.Collections.Generic;
using Lockstep.Math;
using Lockstep.Game;
using NetMsg.Common;
using Debug = Lockstep.Logging.Debug;
using Profiler = Lockstep.Util.Profiler;

namespace Lockstep.Game {
    public class World : BaseSystem {
        public static World Instance { get; private set; }
        public int Tick;
        public List<PlayerInput> PlayerInputs = new List<PlayerInput>();
        public static Player MyPlayer;
        public static object MyPlayerTrans => MyPlayer?.engineTransform;
        private List<BaseSystem> _systems = new List<BaseSystem>();
        private bool _hasStart = false;
        public int HashCode;

        public void RollbackTo(int tick, int missFrameTick, bool isNeedClear = true){
            Debug.Log($" curTick {Tick} RevertTo {tick} {missFrameTick} {isNeedClear}");
        }

        public void StartSimulate(IServiceContainer serviceContainer, IManagerContainer mgrContainer){
            Instance = this;
            _serviceContainer = serviceContainer;
            RegisterSystems();
            if (!serviceContainer.GetService<IConstStateService>().IsVideoMode) {
                RegisterSystem(new TraceLogSystem());
            }

            InitReference(serviceContainer, mgrContainer);
            foreach (var mgr in _systems) {
                mgr.InitReference(serviceContainer, mgrContainer);
            }

            foreach (var mgr in _systems) {
                mgr.DoAwake(serviceContainer);
            }

            DoAwake(serviceContainer);
            foreach (var mgr in _systems) {
                mgr.DoStart();
            }

            DoStart();
        }

        public void StartGame(Msg_G2C_GameStartInfo gameStartInfo, int localPlayerId){
            if (_hasStart) return;
            _hasStart = true;
            var playerInfos = gameStartInfo.UserInfos;
            var playerCount = playerInfos.Length;
            string _traceLogPath = "";
#if UNITY_STANDALONE_OSX
            _traceLogPath = $"/tmp/LPDemo/Dump_{localPlayerId}.txt";
#else
            _traceLogPath = $"c:/tmp/LPDemo/Dump_{Instance.localPlayerId}.txt";
#endif
            Debug.TraceSavePath = _traceLogPath;
            var allPlayers = _gameStateService.GetPlayers();
            allPlayers.Clear();

            Debug.Trace("CreatePlayer " + playerCount);
            //create Players 
            for (int i = 0; i < playerCount; i++) {
                var PrefabId = 0; //TODO
                var initPos = LVector2.zero; //TODO
                var player = _gameStateService.CreateEntity<Player>(PrefabId, initPos);
                player.localId = i;
                allPlayers.Add(player);
            }

            for (int i = 0; i < playerCount; i++) {
                allPlayers[i].input = PlayerInputs[i];
            }

            MyPlayer = allPlayers[localPlayerId];
        }


        public void Simulate(bool isNeedGenSnap = true){
            Step();
        }

        public void Predict(bool isNeedGenSnap = true){
            Step();
        }

        public void CleanUselessSnapshot(int checkedTick){ }


        public override void DoDestroy(){
            foreach (var mgr in _systems) {
                mgr.DoDestroy();
            }

            Debug.FlushTrace();
        }


        public override void OnApplicationQuit(){
            DoDestroy();
        }


        private void Step(){
            var deltaTime = new LFloat(true, 30);
            foreach (var system in _systems) {
                if (system.enable) {
                    system.DoUpdate(deltaTime);
                }
            }

            Tick++;
        }

        public void RegisterSystems(){
            RegisterSystem(new HeroSystem());
            RegisterSystem(new EnemySystem());
            RegisterSystem(new PhysicSystem());
            RegisterSystem(new HashSystem());
        }

        public void RegisterSystem(BaseSystem mgr){
            _systems.Add(mgr);
        }
    }
}