using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QFramework.Car
{
    public class GameSystem : AbstractSystem
    {
        private GameModel m_gameModel;
        private PlayerModel m_playerModel;
        private GameObject m_enemyPrefab;
        private GameObject m_startCamera;
        private GameObject m_focusCamera;

        private void GetGameControllerAndInit()
        {
            m_startCamera = GameObject.Find("StartCamera");
            m_focusCamera = GameObject.Find("FocusCamera");
            m_startCamera.SetActive(true);
            m_focusCamera.SetActive(false);
        }

        protected override void OnInit()
        {
            m_gameModel = this.GetModel<GameModel>();
            m_playerModel = this.GetModel<PlayerModel>();
            m_enemyPrefab = ResLoader.Allocate().LoadSync<GameObject>("Enemy");
            GetGameControllerAndInit();

            m_gameModel.ResetAllValue.Register(() =>
            {
                GetGameControllerAndInit();
            });

        }

        public void GameStart()
        {
            //镜头切换完成后才开始
            ActionKit.Sequence()
               .Callback(() =>
               {
                   m_startCamera.SetActive(false);
                   m_focusCamera.SetActive(true);
               })
               .Delay(0.8f)
               .Callback(() =>
               {
                   AudioKit.PlayMusic("Wasteland Combat Loop");
                   UIKit.OpenPanel("UIRunningPanel");
                   UIKit.OpenPanel("StorePanel");

                   m_gameModel.BeforeGameStart.Trigger();
                   m_gameModel.GameState.Value = GameStates.isRunning;
                   m_gameModel.AfterGameStart.Trigger();

                   this.GetModel<ItemModel>().EquipItem(0);//装备初始武器
                   GameController.Instance.StartCoroutine(SpawnEnemies());
               })
               .Start(GameController.Instance);
        }

        public void GamePause()
        {
            AudioKit.PauseMusic();
            m_gameModel.GameState.Value = GameStates.isPaused;
        }

        public void GameResume()
        {
            AudioKit.ResumeMusic();
            m_gameModel.GameState.Value = GameStates.isRunning;
        }

        public void GameOver()
        {
            m_gameModel.GameState.Value = GameStates.isOver;
            GameController.Instance.StopAllCoroutines();

            ActionKit.Sequence()
               .Callback(() =>
               {
                   AudioKit.Settings.IsMusicOn.Value = false;
               })
               .Delay(0.2f)
               .Callback(() =>
               {
                   UIKit.OpenPanel("UIEndPanel");
               })
               .Start(GameController.Instance);

        }

        private IEnumerator SpawnEnemies()
        {
            while (true)
            {
                //游戏暂停或达到上限时不生成
                if (m_gameModel.GameState != GameStates.isRunning
                    || m_gameModel.EnemyList.Count >= ValueCalculateCenter.GetEnemyCountLimit())
                {
                    yield return null;
                    continue;
                }

                // 检查是否在其他Enemy的安全范围内
                bool isOverlapped = false;
                int cnt = 10;
                while (!isOverlapped && cnt > 0)
                {
                    // 随机位置
                    Vector3 spawnPos;
                    Vector2 randomVec = Random.onUnitSphere;
                    randomVec = randomVec.normalized;
                    spawnPos.x = randomVec.x * m_gameModel.SpawnSafePlayerRadius + m_playerModel.PlayerTrans.position.x;
                    spawnPos.y = m_gameModel.SpawnHeight;
                    spawnPos.z = randomVec.y * m_gameModel.SpawnSafePlayerRadius + m_playerModel.PlayerTrans.position.z;
                    //print("spawnPos" + spawnPos);

                    for (int i = 0; i < m_gameModel.EnemyList.Count; i++)
                    {
                        if (Vector3.Distance(spawnPos, m_gameModel.EnemyList[i].transform.position) < m_gameModel.SpawnSafeEnemyRadius)
                        {
                            isOverlapped = true;
                            break;
                        }
                    }

                    // 如果不在安全范围内,生成Enemy
                    if (!isOverlapped)
                    {
                        GameObject enemy = GameObject.Instantiate(m_enemyPrefab, spawnPos, Quaternion.identity);
                        m_gameModel.EnemyList.Add(enemy.GetComponent<Enemy>());
                        break;
                    }

                    cnt--;
                    //print(cnt);
                }

                yield return new WaitForSeconds(ValueCalculateCenter.GetSpawnInterval());
            }
        }
    }
}