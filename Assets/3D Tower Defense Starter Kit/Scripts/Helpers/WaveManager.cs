/*  This file is part of the "3D Tower Defense Starter Kit" project by Rebound Games.
 *  You are only allowed to use these resources if you've bought them directly or indirectly
 *  from Rebound Games. You shall not license, sublicense, sell, resell, transfer, assign,
 *  distribute or otherwise make available to any third party the Service or the Content. 
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;


//WaveManager.cs handles spawning of waves and wave properties
[System.Serializable]
public class WaveManager : MonoBehaviour
{
    //store wave properties, this is a list of an own class (see below)
    [HideInInspector]
    public List<WaveOptions> options = new List<WaveOptions>();
	//wave start option to handle the behaviour and time of new waves
    public enum WaveStartOption
    {
        waveCleared, 	//wait till current wave is cleared
        interval,		//wait defined interval
        userInput,		//wait for the player to start next wave
        roundBased		//wait till current wave is over and player input
    }
    //default wave start option variable
    public WaveStartOption waveStartOption = WaveStartOption.waveCleared;

    //delay between two waves - break / time to relax few seconds
    public int secBetweenWaves;
	//increasing seconds between waves on wave start option 'interval'
    public int secIncrement;

    //auto start waves at scene launch
    public bool autoStart;

    public WaveAnimations anims = new WaveAnimations();
    public WaveSounds sounds = new WaveSounds();

    //seconds till next wave starts, (between waves)
    //used on wave start option 'interval' and 'waveCleared'
    //accessed from GameInfo.cs to display the wave timer
    [HideInInspector]
    public float secTillWave = 0;

    //boolean value for allowing the player to start the next wave
    //only used on wave start option 'userInput', this gets toggled after all enemies
    //of the current wave are spawned so the player is able to start the next wave immediately
    //via our start button in GameInfo.cs 
    [HideInInspector]
    public bool userInput = true;


    void Start()
    {
        //set total wave count of GameHandler.cs so that GameInfo.cs can display it within our GUI
        GameHandler.waveCount = options.Count;

        //play background music as soon as the game starts
        AudioManager.Play(sounds.backgroundMusic);

        //start waves immediately on game launch ( no extra button )
        if(autoStart)
            StartWaves();
        
    }


    //method to launch waves, called by GameInfo.cs (start button)
    public void StartWaves()
    {
        //start first wave
        StartCoroutine(LaunchWave());
    }


    //here we check the state of our game and perform different actions per state
    void CheckStatus()
    {
        //there are waves left, continue
        if (GameHandler.wave < options.Count)
        {
            //not all enemies are dead/removed, do nothing and return
            if (GameHandler.enemiesAlive > 0)
                return;

			//handle different wave start options
			//(we only implemented 'waveCleared')
            switch (waveStartOption)
            {
            	//all enemies are dead and chosen option is 'wait till wave is cleared'
            	//this part gets executed with its specific properties
                case WaveStartOption.waveCleared:
					//increase seconds between waves (if secIncrement is greater than 0),
					//starting at the 2nd wave
                    if (GameHandler.wave > 0)
                        secBetweenWaves += secIncrement;

					//set and reduce wave timer with current value
					//(here wave timer starts if the current wave is over)
                    StartCoroutine("WaveTimer", secBetweenWaves);

                    //start next wave in defined seconds
                    Invoke("StartWaves", secBetweenWaves);
                    break;

				//we skip other options, they don't have specific properties
                    /*
                case WaveStartOption.interval:
                case WaveStartOption.userInput:
                case WaveStartOption.roundBased:
                    break;
                    */
            }

            Debug.Log("Wave Defeated");
            
			//play background music between waves
            AudioManager.Play(sounds.backgroundMusic);
            //play "wave end" sound one time
            AudioManager.Play2D(sounds.waveEndSound);
        }
        else
        {
            //to the given time - at the last enemy of the last wave 
            //this step repeats checking if all enemies are dead and therefore the game is over
            //if so, it sets the gameOver flag to true
            //GameHandler.cs then looks up our health points and determines win or lose

            if (GameHandler.enemiesAlive > 0)
                return;

			//play background music on game end
            AudioManager.Play(sounds.backgroundMusic);

			//play "wave end" sound one time
            AudioManager.Play2D(sounds.waveEndSound);

			//toggle gameover flag
            GameHandler.gameOver = true;
        }

        //cancel repeating CheckStatus() calls after the part above hasn't returned
        //this means we executed a complete CheckStatus() call and either started a new
        //wave or ended the game
        CancelInvoke("CheckStatus");
    }


    //this method launches a new / next wave
    IEnumerator LaunchWave()
    {
        Debug.Log("Wave " + (GameHandler.wave + 1) + " launched! StartUp GameTime: " + Time.time);

        //play battle music while fighting
        AudioManager.Play(sounds.battleMusic);

        //play wave start sound
        AudioManager.Play2D(sounds.waveStartSound);

        //play spawn start animation
        if (anims.spawnStart)
        {
            anims.objectToAnimate.animation.Play(anims.spawnStart.name);
            //wait until spawn animation ended before spawning enemies
            yield return new WaitForSeconds(anims.spawnStart.length);
        }

        //spawn independent coroutines for every enemy type (row) defined in the current wave,
        //or in other words, go through the wave and start a coroutine for each row in this wave
        for (int i = 0; i < options[GameHandler.wave].enemyPrefab.Count; i++)
        {
            StartCoroutine(SpawnEnemyWave(i));
        }

        //only invoke the wave status check ("CheckStatus()") when we need it
        //CheckStatus() checks if all enemies in this wave are defeated or it's the last wave etc.
        //this method gets called when all enemies are successfully spawned of this wave
		//here we get the seconds at what time this happens
        float lastSpawn = GetLastSpawnTime(GameHandler.wave);

		//cancel running CheckStatus calls, below we start them again
        if (IsInvoking("CheckStatus"))
            CancelInvoke("CheckStatus");

		//handle different wave start option
        switch (waveStartOption)
        {
        	//'waveCleared' and 'roundBased' don't have specific properties, skip them
        	/*
            case WaveStartOption.waveCleared:
            case WaveStartOption.roundBased:
                break;
			*/
			
			//on 'interval' we start the next wave immediately in 'secBetweenWaves' seconds
			//ignoring the current wave status and enemies
            case WaveStartOption.interval:
            	//increase seconds between the following waves
                if (GameHandler.wave > 0)
                    secBetweenWaves += secIncrement;

				//until the last wave
                if (GameHandler.wave + 1 < options.Count)
                {
                	//set and reduce wave timer with current value
					//(here the wave timer starts automatically with every new wave)
                    StartCoroutine("WaveTimer", secBetweenWaves);
                    //start new wave in 'secBetweenWaves' seconds
                    Invoke("StartWaves", secBetweenWaves);
                }
                break;
                
            //on option 'userInput', at wave start the boolean 'userInput' is set to false
            //so the player isn't able to start new waves anymore until all enemies are spawned
            //we call "ToggleInput" at the last spawned enemy time which toggles userInput again
            case WaveStartOption.userInput:
                userInput = false;
                Invoke("ToggleInput", lastSpawn);
                break;
        }

		//repeat CheckStatus calls every 2 seconds after the last enemy of this wave
        InvokeRepeating("CheckStatus", lastSpawn, 2f);
        
        //invoke spawn end animation at the given time
        Invoke("PlaySpawnEndAnimation", lastSpawn);

        //increase wave index
        GameHandler.wave++;
    }


    //play spawn end animation, invoked by CheckStatus() on the last enemy
    void PlaySpawnEndAnimation()
    {
        if (anims.spawnEnd)
        {
            anims.objectToAnimate.animation.Play(anims.spawnEnd.name);
        }
    }

	
	//toggle userInput after the last enemy of the current wave
	//(the player is able to start a new wave on wave start option 'userInput')
    void ToggleInput()
    {
        userInput = true;
    }


	//this method reduces the seconds variable till the next wave,
	//then this value gets displayed on the screen by GameInfo.cs
    IEnumerator WaveTimer(float seconds)
    {
    	//store passed in seconds value and add current playtime
    	//to get the targeted playtime value
        float timer = Time.time + seconds;

		//while the playtime hasn't reached the desired playtime
        while (Time.time < timer)
        {
        	//get actual seconds till next wave by subtracting calculated and current playtime
        	//this value gets rounded to two decimals
            secTillWave = Mathf.Round((timer - Time.time) * 100f) / 100f;
            yield return true;
        }

		//when the time is up we set the calculated value exactly back to zero
        secTillWave = 0f;
    }


    //this method spawns all enemies for one wave
    //the given parameter defines the position in our wave editor lists
    //-(the enemy row of our current wave)
    IEnumerator SpawnEnemyWave(int index)
    {
        //store row index because it could change over the next time delay
        int waveNo = GameHandler.wave;
        //delay spawn at start (seconds defined in List "startDelay")
        yield return new WaitForSeconds(Random.Range(options[waveNo].startDelayMin[index], options[waveNo].startDelayMax[index]));

        //Debug.Log("Spawning " + EnemyCount[index] + " " + EnemyPref[index].name + " on Path: " + Path[index]);

        //instantiate the entered count of enemies of this row
        for (int j = 0; j < options[waveNo].enemyCount[index]; j++)
        {
            //print error if prefab is not set and abort
            if (options[waveNo].enemyPrefab[index] == null)
            {
                Debug.LogWarning("Enemy Prefab not set in Wave Editor!");
                yield return null;
            }

            //if this row has no path Container assigned (enemies don't get a path), debug a warning and return
            if (options[waveNo].path[index] == null)
            {
                Debug.LogWarning(options[waveNo].enemyPrefab[index].name + " has no path! Please set Path Container.");
                break;
            }

            //all checks were passed, spawn this enemy
            SpawnEnemy(waveNo, index);
                
            //delay the spawn time between enemies (seconds defined in List "delayBetween")
            yield return new WaitForSeconds(Random.Range(options[waveNo].delayBetweenMin[index],options[waveNo].delayBetweenMax[index]));
        }
    }


    void SpawnEnemy(int waveNo, int index)
    {
        //get prefab and first waypoint position of this enemy and its path
        GameObject prefab = options[waveNo].enemyPrefab[index];
        Vector3 position = options[waveNo].path[index].waypoints[0].position;

        //instantiate/spawn enemy from PoolManager class with its configs
        GameObject enemy = PoolManager.Pools["Enemies"].Spawn(prefab, position, Quaternion.identity);
        //get the TweenMove component so we can set the corresponding path container to follow
        enemy.GetComponentInChildren<TweenMove>().pathContainer = options[waveNo].path[index];
        //increase alive enemy count by one
        GameHandler.enemiesAlive++;
    }


    //this method calculates the maximum time for spawning an enemy per wave and returns it
    //so we don't call CheckStatus() until we need to
    float GetLastSpawnTime(int wave)
    {
        //time result variable to return
        float lastSpawn = 1;

        //loop through all enemy rows in this active wave and calculate spawn time
        //store the highest spawn time
        for (int i = 0; i < options[wave].enemyCount.Count; i++)
        {
            //last enemy spawn time output, comment out to see details
            /*
            Debug.Log("Overall spawn time for enemy: " + i + " in wave " + wave + " === " + "delay " + (Delay[indx + i] 
                        + " multiply " + (EnemyCount[indx+i] - 1) * DelayBetw[indx + i]) + " === "
                        + (Delay[indx + i] + (EnemyCount[indx + i] - 1) * DelayBetw[indx + i]) + " seconds.");
            */

            //add each possible delay, to calculate final spawn time for this row
            //the final time consists of the delay at start and time between all enemies
            float result = options[wave].startDelayMax[i] + (options[wave].enemyCount[i] - 1) * options[wave].delayBetweenMax[i];

            //save result if higher spawn time was found
            if (result > lastSpawn)
            {
                //add a quarter second at the end so CheckStatus() doesn't get called at the same frame
                lastSpawn = result + 0.25f;
            }
        }

        //give result back
        return lastSpawn;
    }
}


[System.Serializable]
public class WaveAnimations
{
    public GameObject objectToAnimate;
    //animation to play on wave start
    public AnimationClip spawnStart;
    //animation to play when spawns ended
    public AnimationClip spawnEnd;
}


[System.Serializable]
public class WaveSounds
{
    //sound to play on wave start
    public AudioClip waveStartSound;
    //sound to play during battle
    public AudioClip battleMusic;
    //sound to play on wave end
    public AudioClip waveEndSound;
    //sound to play during breaks
    public AudioClip backgroundMusic;
}


//Wave Options class - per wave
[System.Serializable]
public class WaveOptions
{
    //enemy prefab to instantiate, multiple enemies per wave possible
    public List<GameObject> enemyPrefab = new List<GameObject>();
    //how many enemies to spawn, per type
    public List<int> enemyCount = new List<int>();
    //spawn delay measured from start, per type
    public List<float> startDelayMin = new List<float>();
    public List<float> startDelayMax = new List<float>();
    //spawn delay between each enemy, per type
    public List<float> delayBetweenMin = new List<float>();
    public List<float> delayBetweenMax = new List<float>();
    //which path each enemy has to follow, per type
    public List<PathManager> path = new List<PathManager>();
}