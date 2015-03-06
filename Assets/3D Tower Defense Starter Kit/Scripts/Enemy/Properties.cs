/*  This file is part of the "3D Tower Defense Starter Kit" project by Rebound Games.
 *  You are only allowed to use these resources if you've bought them directly or indirectly
 *  from Rebound Games. You shall not license, sublicense, sell, resell, transfer, assign,
 *  distribute or otherwise make available to any third party the Service or the Content. 
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//Enemy Properties and Damage/Slow/Death Methods
public class Properties : MonoBehaviour
{
    public float health = 100;     //health points
    private float maxhealth;     //variable for caching start health
    public GameObject healthbar;    //3D health indicator prefab
    public GameObject hitEffect;    //particle effect to show if enemy gets hit
    public GameObject deathEffect;  //particle effect to show on enemy death
    public AudioClip hitSound;   //sound to play on hit via AudioManager
    public AudioClip deathSound; //sound to play on death via AudioManager
    //animation component
    [HideInInspector]
    public Animation anim;
    public AnimationClip walkAnim;  //animation to play during walk time
    public AnimationClip dieAnim;  //animation to play on death
	public AnimationClip successAnim; //animation to play on end of path

    public float[] pointsToEarn;  //points we get if we kill this enemy, array to support multiple resources
    public int damageToDeal = 1;    //game health we lose if this enemy reaches its destination
    private TweenMove myMove; //store movement component of this object for having access to movement
    //list to cache references for all towers whose range we entered
    [HideInInspector]
    public List<TowerBase> nearTowers = new List<TowerBase>();
    //time value to delay instantiation/activation of particle effects
    //(we do not want to spawn a new effect on every hit as this drastically decreases performance)
    private float time;


    void Start()
    {
        //add reference of this script to PoolManager's Properties dictionary,
        //this gets accessed by Projectile.cs on hit so we don't need a GetComponent call every time
        PoolManager.Props.Add(gameObject.name, this);

        //get movement reference
        myMove = gameObject.GetComponent<TweenMove>();
        //get animation component
        anim = gameObject.GetComponentInChildren<Animation>();
        //store start health for later use (resetted on Despawn())
        maxhealth = health;
    }


    //on every spawn, play walk animation if one is set via the inspector
    IEnumerator OnSpawn()
    {
        yield return new WaitForEndOfFrame();

        if (walkAnim)
		{
			//randomize playback offset and play animation
            anim[walkAnim.name].time = Random.Range(0f, anim[walkAnim.name].length);
            anim.Play(walkAnim.name);
		}
    }


    //reposition/center 3D healthbar so it always looks at our game camera
    void LateUpdate()
    {
        if (healthbar)
            healthbar.transform.LookAt(Camera.main.transform);
    }


    //we entered the range of a tower, add it to our nearTowers dictionary
    public void AddTower(TowerBase tower)
    {
        nearTowers.Add(tower);
    }


    //remove towers which are too far away
    //called by RangeTrigger.cs on OnTriggerExit()
    public void RemoveTower(TowerBase tower)
    {
        nearTowers.Remove(tower);
    }


    //we got hit by a projectile
    public void Hit(float damage)
    {
        //check if our health points are greater than zero and we aren't dying, else return
        if (health <= 0 || (dieAnim && anim.IsPlaying(dieAnim.name)))
        return;

        //reduce health by projectile damage amount
        health -= damage;

        //check whether we survived this hit
        //only call OnHit() if it wasn't called within the last 2 seconds (reason defined above)
        if (health > 0 && Time.time > time + 2)
            OnHit();
        else if (health <= 0)
        {
            //loop through pointsToEarn and add all points to our resources array for this enemy kill
            for (int i = 0; i < pointsToEarn.Length; i++)
            {
                GameHandler.SetResources(i, pointsToEarn[i]);
            }
            //track the correct amount of enemies alive
            GameHandler.EnemyWasKilled();
            //remove enemy
            OnDeath();
        }

        //adjust healthbar texture offset
        //create new vector2 variable
        if (healthbar)
        {
            Vector2 barOffset = Vector2.zero;
            //calculate life in percentage and set that as horizontal texture offset (x value)
            barOffset.x = (health / maxhealth) * 0.5f;
            //apply calculated offset to 3D healthbar texture
            healthbar.renderer.material.SetTextureOffset("_MainTex", barOffset);
        }
    }


    //play hit sound and instantiate hit particle effect
    void OnHit()
    {
        //set current time value,
        //so this method doesn't get called the next 2 seconds anymore
        time = Time.time;

        //play hit sound via AudioManager
        //add some random pitch so it sounds differently on every hit 
        AudioManager.Play(hitSound, transform.position, Random.Range(1.0f - .2f, 1.0f + .1f));

        //instantiate hit effect at object's position if one is set
        if (hitEffect)
            PoolManager.Pools["Particles"].Spawn(hitEffect, transform.position, hitEffect.transform.rotation);
    }


    //OnDeath() stops movement, removes us from other towers, grants money for this kill,
    //plays a death sound and instantiates a particle effect before despawn
    void OnDeath()
    {
        //stop all running tweens (through TweenMove) and coroutines
        myMove.StopAllCoroutines();
        myMove.tween.Kill();
        StopAllCoroutines();

        //say to all towers that this enemy died/escaped
        //despawn this enemy
        StartCoroutine("RemoveEnemy");
    }


    //slow method - called by Projectile.cs
    //passed arguments are time and slow factor
    public void Slow(float slowTime, float slowFactor)
    {
        //don't apply slow if this enemy died already or was despawned
        if (health <= 0 || !gameObject.activeInHierarchy
            || (dieAnim && anim.IsPlaying(dieAnim.name)))
            return;

        //store maximum speed of movement component
        float maxSpeed = myMove.maxSpeed;

        //the projectile that hit this object wants to slow it down -
        //we need to check if our current speed is faster than the slowed down speed,
        //so this impact would skip weaker slows
        if (myMove.speed >= maxSpeed * slowFactor)
        {
            //set new speed with slow applied 
            myMove.speed = maxSpeed * slowFactor;

            //stop (existing?) slow
            myMove.StopCoroutine("Slow");

            //start new slow coroutine and pass in new slow time
            myMove.StartCoroutine("Slow", slowTime);
        }
    }


    //damage over time method - called by Projectile.cs
    //passed arguments are damage, time and frequency
    public IEnumerator DamageOverTime(float[] vars)
    {
        //we don't need any health checks here, because Projectile.cs
        //checks that already - to not start a new coroutine at all

        /*
            short example what we would like to see:
            vars[0] = total damage over time = 5
            vars[1] = time = 6
            vars[2] = frequency = 4
         
            delay = time between damage calls = 2 seconds
            dotDmg = damage per call = 1.25
            
            result:
            1st damage at 0 seconds = 1.25
            2nd damage at 2 seconds = 1.25
            3rd damage at 4 seconds = 1.25
            4th damage at 6 seconds = 1.25
            ------------------------------
            total damage: 5 over 6 seconds
        */

        //calculate interval of damage calls
        //we directly apply the first damage on impact,
        //therefore we reduce one frequency before
        float delay = vars[1] / (vars[2]-1);
        //calculate resulting damage per call
        float dotDmg = vars[0] / vars[2];

        //instantly apply first damage
        Hit(dotDmg);

        //yield through all further damage calls based on frequency
        for (int i = 0; i < vars[2]-1; i++)
        {
            //wait interval for next hit
            yield return new WaitForSeconds(delay);
            //apply next damage
            Hit(dotDmg);
        }

        //search particle gameobject which was parented to this burning enemy 
        foreach (Transform child in transform)
        {
            //we use a general "(Clone)" search because spawned particle effects
            //contain this string and there really shouldn't be any other clone parented
            if (child.name.Contains("(Clone)"))
                PoolManager.Pools["Particles"].Despawn(child.gameObject);
        }
    }


    //we reached our destination, called by TweenMove.cs
    void PathEnd()
    {
        //we couldn't stop this enemy, do damage to our game health points
        GameHandler.DealDamage(damageToDeal);
        //remove enemy
        OnDeath();
    }


    //before this enemy gets despawned (on death or in case it reached its destination),
    //say to all towers that this enemy died
    IEnumerator RemoveEnemy()
    {
        //remove ourselves from each tower's inRange list
        for (int i = 0; i < nearTowers.Count; i++)
        {
            nearTowers[i].inRange.Remove(gameObject);
        }

        //clear our inRange list
        nearTowers.Clear();

        //remove possible 'damage over time' particle effects (see DamageOverTime())
        foreach (Transform child in transform)
        {
            if (child.name.Contains("(Clone)"))
                PoolManager.Pools["Particles"].Despawn(child.gameObject);
        }

        //if this object used the Progress Map (in TweenMove.cs)
        if (myMove.pMapProperties.enabled)
        {
            //stop calculating our ProgressMap path progress
            myMove.CancelInvoke("ProgressCalc");

            //call method to remove it from the map 
            ProgressMap.RemoveFromMap(myMove.pMapProperties.myID);
        }

        //this enemy died, handle death stuff
        if (health <= 0)
        {
            //if set, instantiate deathEffect at current position
            if (deathEffect)
                PoolManager.Pools["Particles"].Spawn(deathEffect, transform.position, Quaternion.identity);

            //play sound on death via AudioManager
            AudioManager.Play(deathSound, transform.position);

            //handle death animation
            //wait defined death animation delay if set
            if (dieAnim)
            {
                anim.Play(dieAnim.name);
                yield return new WaitForSeconds(dieAnim.length);
            }
        }
        //the enemy didn't die, and escaped instead 
        else
        {
            if (successAnim)
            {
                anim.Play(successAnim.name);
                yield return new WaitForSeconds(successAnim.length);
            }
        }

        //reset all initialized variables for later reuse
        //set start health back to maximum health
        health = maxhealth;
        //reset 3D healthbar offset so it indicates 100% health points
        if (healthbar)
            healthbar.renderer.material.SetTextureOffset("_MainTex", new Vector2(0.5f, 0));

        //despawn/disable us
        PoolManager.Pools["Enemies"].Despawn(gameObject);
    }
}