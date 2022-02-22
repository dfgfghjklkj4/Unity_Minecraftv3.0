using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.PostProcessing;
using System;
using Random = UnityEngine.Random;

using System.Security.Permissions;
using System.Globalization;
using XLua;




[Serializable]
public class FogSettings
{
    public Color fogColor;
    public bool fog;
    public FogMode fogMode;
    public float fogDensity;

    public FogSettings()
    {
        // this.fogColor = RenderSettings.fogColor;
        //  this.fog = RenderSettings.fog;
        //  this.fogMode = RenderSettings.fogMode;
        //  this.fogDensity = RenderSettings.fogDensity;
    }

    public void Set()
    {
        RenderSettings.fog = fog;
        RenderSettings.fogColor = fogColor;
        RenderSettings.fogMode = fogMode;
        RenderSettings.fogDensity = fogDensity;
    }
}

[LuaCallCSharp]
public class PlayerController : MonoBehaviour
{


    public static PlayerController Instance;
    public CharacterController characterController;
    public Camera camera;
    public MonitorController monitorController;
    public HotbarController hotbarController;
    public PostProcessVolume postProcessVolume;
    public ChunkManager chunkManager;
    public Transform body;
    public Transform feet;
    public AudioSource stepAudioSource;
    public AudioClip teleportClip;
    public AudioClip hitClip;
    public AudioClip fallLandSmallClip;
    public AudioClip fallLandLargeClip;
    public AudioClip fallingClip;
    public GameObject canvas;
    [Range(0.0f, 100f)] public float baseSpeed = 10f;
    [Range(0.0f, 100f)] public float jumpPower = 3f;
    [Range(0.0f, 100f)] public float enderPearlThrowingPower = 10f;
    [Range(0.0f, 1.0f)] public float sidewaysSpeedModifier = 0.5f;
    [Range(0.0f, 2.0f)] public float waterSpeedModifier = 0.5f;
    public ParticleSystem splashEffect;
    public ParticleSystem explosionEffect;
    public ParticleSystem treeDownEffect;
    public ParticleSystem breakEffect;
    public ParticleSystem teleportEffect;
    public ParticleSystem growthEffect;
    public TNT tntPrefab;

    //private Color initialSkyColor;
    public Material[] materialsToColor;



    public float _speed;


    public BlockType _prevFeetType;



    private bool _isJumping;
    private float _jumpTimer;
    private float _jumpYVelocity;
    private float _terminalVelocity = 30.0f;
    [SerializeField] private float _jumpDuration;

    [SerializeField] private bool _isInWater;
    //[SerializeField] private bool _cameraIsInWater = false;

    // Mouse
    public float mouseSensitivity = 100.0f;
    public float clampAngle = 80.0f;

    private float rotY = 0.0f; // rotation around the up/y axis
    private float rotX = 0.0f; // rotation around the right/x axis

    private bool _prevIsGrounded = true;

    private Vector3Int _prevCameraBlockPos;

    private float _movementSincePrevStep = 0.0f;




    private void Awake()
    {
        if (!Instance)
        {
            Instance = this;
        }
    }
    void Start()
    {


        TNT.key = tntPrefab.GetHashCode();

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;


        //initialSkyColor = RenderSettings.fogColor;

        _speed = baseSpeed;

        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
        }

        // Mouse Movement
        Vector3 rot = camera.transform.localRotation.eulerAngles;
        rotY = rot.y;
        rotX = rot.x;

        // PlacePlayerOnSurface(false);

        keycodes = new KeyCode[] {
            KeyCode.Alpha1,
            KeyCode.Alpha2,
            KeyCode.Alpha3,
            KeyCode.Alpha4,
            KeyCode.Alpha5,
            KeyCode.Alpha6,
            KeyCode.Alpha7,
            KeyCode.Alpha8,
            KeyCode.Alpha9
        };
    }


  


    Vector3 lastPos;

    private void FixedUpdate()
    {
        if (lastPos != transform.position)
        {
            float speed = Vector3.Distance(lastPos, transform.position) / Time.fixedDeltaTime;
            //   tm.text = m + speed + m2;
            lastPos = transform.position;
        }

    }
    public BlockType currentType;
    public FlyFree fly;
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            characterController.enabled = false;
            this.enabled = false;
            transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, transform.localEulerAngles.y, 0);
            camera.transform.localEulerAngles = Vector3.zero;
            fly.enabled = true;
        }

        HandleHotbarSelection();





        monitorController.OnBlockTraveled();


        WorldChunk chunkfeet = null;
        // Check if feet in water
        Vector3Int pos = ChunkManager.GetPlayerPosition(feet.transform);
        //  Vector3Int pos = Vector3Int.RoundToInt(hit.point + offset);
        currentType = chunkManager.GetBlockAtPosition(pos, ref chunkfeet);
        // BlockType currentType = currBlock == null ? null : currBlock.type;
        if (_prevFeetType != currentType)
        {

            BlockType blockTypeName = BlockType.IsAirBlock(currentType) ? null : currentType;
            if (blockTypeName != null && blockTypeName.blockName == BlockNameEnum.Water)
            {
               OnEnterWater();
            }
            else if (!BlockType.IsAirBlock(_prevFeetType) && _prevFeetType.blockName == BlockNameEnum.Water)
            {
               OnExitWater();
            }

            _prevFeetType = currentType;
        }



      

        Vector3Int cameraPos = ChunkManager.GetPlayerPosition(camera.transform);
        if (cameraPos != _prevCameraBlockPos)
        {
            WorldChunk chunk = null;
            BlockType prevCameraBlock = chunkManager.GetBlockAtPosition(_prevCameraBlockPos, ref chunk);
            BlockType cameraBlock = chunkManager.GetBlockAtPosition(cameraPos, ref chunk);
            if (BlockType.IsAirBlock(cameraBlock) && (!BlockType.IsAirBlock(prevCameraBlock) && prevCameraBlock.blockName == BlockNameEnum.Water))
            {
             OnCameraExitWater();
            }
            else if (BlockType.IsAirBlock(prevCameraBlock) && (!BlockType.IsAirBlock(cameraBlock) && cameraBlock.blockName == BlockNameEnum.Water))
            {
              OnCameraEnterWater();
             
            }
            _prevCameraBlockPos = cameraPos;
        }




        ///////////////////////////////////////////////////////////////////////////

        if (_prevIsGrounded != characterController.isGrounded)
        {
            _prevIsGrounded = characterController.isGrounded;
            if (characterController.isGrounded == false)
            {
                _isJumping = true;
                _jumpTimer = 0.0f;
            }
        }

       

        if (Input.GetMouseButtonDown(1))
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                Touch[] touches = Input.touches;
                if (touches.Length == 2)
                {
                    float dis = Vector2.Distance(touches[0].position, touches[1].position);
                    if (dis < 200)
                    {
                        HandleRightMouseClick();
                    }
                }

            }
            else
            {
                var mp = camera.ScreenToViewportPoint(Input.mousePosition);
                //  if (mp.x > 0.25f && mp.x < 0.75f && mp.y > 0.25f && mp.y < 0.75f)
                {


                    HandleRightMouseClick();
                }
            }


        }
        else if (Input.GetKeyDown(KeyCode.F))
        {

            BreakBlock();


        }
        else if (Input.GetMouseButtonDown(0))
        {


            if (Application.platform == RuntimePlatform.Android)
            {
                Touch[] touches = Input.touches;
                if (touches.Length == 1)
                {
                    var mp = camera.ScreenToViewportPoint(Input.mousePosition);
                    if (mp.x > 0.25f && mp.x < 0.75f && mp.y > 0.25f && mp.y < 0.75f)
                    {
                        BreakBlock();
                    }
                }

            }

            else
            {
                var mp = camera.ScreenToViewportPoint(Input.mousePosition);
                if (mp.x > 0.25f && mp.x < 0.75f && mp.y > 0.25f && mp.y < 0.75f)
                {
                    BreakBlock();
                }
            }


        }

        else if (Input.GetKeyDown(KeyCode.Q))
        {
            LaunchTNT();
        }
    

   

      

        if (_isInWater)
        {
            _jumpYVelocity /= 2.0f;
        }

        if (_isJumping)
        {
            _jumpTimer += Time.deltaTime;
        }

        if (characterController.isGrounded && characterController.enabled)
        {
            if (_isJumping)
            {
                _isJumping = false;
                if (_jumpYVelocity < -15.0f)
                {
                    AudioClip clip = _jumpYVelocity < -29f ? fallLandLargeClip : fallLandSmallClip;
                    // AudioSource.PlayClipAtPoint(clip, this.feet.position, 1.0f);
                }

                //  stepAudioSource.volume = 1.0f;
                if (stepAudioSource.isPlaying && stepAudioSource.clip != null)
                {
                    // Stop the falling air sound
                    stepAudioSource.Stop();
                }
            }
            _jumpYVelocity = 0.0f;
        }

        if (_isInWater)
        {
            _jumpTimer = 0.0f;
        }

        if (_isJumping && characterController.enabled)
        {
            if (_jumpTimer > 0.8f && stepAudioSource.isPlaying == false)
            {
                if (characterController.enabled)
                {

                    // stepAudioSource.clip = fallingClip;
                    //stepAudioSource.Play();


                }

            }
            if (stepAudioSource.clip != null)
            {
                //  stepAudioSource.volume = Mathf.InverseLerp(0.8f, 2.0f, _jumpTimer);
            }
        }

        if (Input.GetKey(KeyCode.Space) && _isInWater == false && characterController.isGrounded)
        {
            _isJumping = true;
            _jumpTimer = 0.0f;
            _jumpYVelocity = jumpPower;
        }

        Vector3 movement = Vector3.zero;

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            movement += this.transform.forward * _speed;
        }
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            movement += -this.transform.forward * _speed;
        }
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            movement += -this.transform.right * _speed * sidewaysSpeedModifier;
        }
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            movement += this.transform.right * _speed * sidewaysSpeedModifier;
        }


        movement = camera.transform.rotation * movement;

        float mag = movement.magnitude;

        movement.y = 0.0f;
        movement = movement.normalized * mag;

        _movementSincePrevStep += mag * Time.deltaTime;

        float gravity = -30f;
        _jumpYVelocity += gravity * Time.deltaTime;
        _jumpYVelocity = Mathf.Clamp(_jumpYVelocity, -_terminalVelocity, float.MaxValue);

        Vector3 jumpVelocity = Vector3.up * _jumpYVelocity;

        movement += jumpVelocity;

        if (Input.GetKey(KeyCode.Space) && _isInWater)
        {

            movement += Vector3.up * 4f;
        }
        else if ((Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.LeftShift)) && _isInWater && characterController.isGrounded == false)
        {
            movement -= Vector3.up * 4f;
        }
        else if (_isJumping == false && characterController.isGrounded == false)
        {
            movement += Vector3.up * gravity * Time.deltaTime;
        }
        if (characterController.enabled)
        {
            characterController.Move(movement * Time.deltaTime);
        }



        //   if (Cursor.lockState == CursorLockMode.Locked)
        {
            if (characterController.enabled)
            {
                DoMouseRotation();
            }

        }

        PlayStepAudio();


    }
   
    private void LateUpdate()
    {
       
        if (characterController.enabled == false)
        { // Re-enable character controller after having enough time to teleport
          // characterController.enabled = true;
        }
        

      
       
    }

    private void PlayStepAudio()
    {
        if (characterController.enabled == false)
        {
            return;
        }

        WorldChunk chunk = null;
        if (_movementSincePrevStep > 3f)
        {
            _movementSincePrevStep = 0.0f;
         
            if (!BlockType.IsAirBlock(currentType))
            {
                stepAudioSource.Play();
              
                AudioClip[] stepClips = currentType.stepClips;
                if (stepClips != null && stepClips.Length > 0)
                {
                     AudioClip clip = stepClips[Random.Range(0, stepClips.Length )];
                     stepAudioSource.clip = clip;
                    stepAudioSource.Play();
               
                }

            }
        }
    }


    private void OnEnterWater()
    {
       
        _isInWater = true;
        _speed = baseSpeed * waterSpeedModifier; // Entering water
        var effect = Instantiate(splashEffect, null);
        effect.transform.position = feet.position;
        Destroy(effect, 1.5f);
    }

    private void OnExitWater()
    {
     
        _isInWater = false;
        _speed = baseSpeed;
        //  SetMaterialColors(Color.white);
        //Camera.main.backgroundColor = initialSkyColor;
    }

    private void OnCameraEnterWater()
    {
         stepAudioSource.Stop();
        UnderWateref.Instanc.gameObject.SetActive(true);
        UnderWateref.Instanc.OnEnterWater();
     
    }

    private void OnCameraExitWater()
    { 
          var effect = Instantiate(splashEffect, null);
        effect.transform.position = feet.position;
        Destroy(effect, 1.5f);
          UnderWateref.Instanc.OnExitWater();
        stepAudioSource.Play();
      
    }

    private void DoMouseRotation()
    {
        if (!characterController.enabled)
        {
            return;
        }
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = -Input.GetAxis("Mouse Y");

        rotY += mouseX * mouseSensitivity * Time.deltaTime;
        rotX += mouseY * mouseSensitivity * Time.deltaTime;

        rotX = Mathf.Clamp(rotX, -clampAngle, clampAngle);

        Quaternion localRotation = Quaternion.Euler(rotX, rotY, 0.0f);
        camera.transform.rotation = localRotation;

        body.rotation = camera.transform.rotation;
        Vector3 angles = body.eulerAngles;
        angles.x = 0.0f;
        angles.z = 0.0f;
        body.eulerAngles = angles;
    }

    private void LaunchTNT()
    {
        TNT tnt = ObjPool.GetComponent<TNT>(tntPrefab, camera.transform.position + camera.transform.forward, Quaternion.identity);
        tnt.init();
        tnt.Launch(camera.transform.forward);
        tnt.FireTNT();
    }







    private void PlaceBlock(BlockType block)
    {

        Vector3Int blockPos;
        if (RaycastToBlock(10.0f, out RaycastHit hit, true, out blockPos))
        {
            Vector3Int feetPos = Vector3Int.RoundToInt(feet.position);
            //   if (blockPos == feetPos || blockPos == (feetPos + Vector3Int.up))
            if (Vector3.Distance(transform.position, hit.point) < 1.51f)
            {
                return; // Don't place block in feet or head space
            }

         
            WorldChunk chunk = ChunkManager.GetNearestChunk(blockPos);

            //    mb.oldBlock = BlockNameEnum.Air;
            Vector3Int lPos = chunk.WorldToLocalPosition(blockPos);
         
          chunk.PlaceBlock(lPos,block);
            PlayBlockSound(block, blockPos);

            monitorController.OnBlockPlaced();
        }
    }

    private void PlaceTNT()
    {

        Vector3Int blockPos;
        if (RaycastToBlock(10.0f, out RaycastHit hit, true, out blockPos))
        {
            Vector3Int feetPos = Vector3Int.RoundToInt(feet.position);
            if (blockPos == feetPos || blockPos == (feetPos + Vector3Int.up))
            {
                return; // Don't place block in feet or head space
            }

            TNT tnt = ObjPool.GetComponent<TNT>(tntPrefab, blockPos, Quaternion.identity);
            tnt.init();
          
        }
    }


    private BlockType GetTargetBlock(float maxDistance)
    {
        Vector3Int blockPos;
        if (RaycastToBlock(maxDistance, out RaycastHit hit, false, out blockPos))
        {
            WorldChunk chunk = null;
            BlockType targetBlock = chunkManager.GetBlockAtPosition(blockPos, ref chunk);
            return targetBlock;
        }

        return null;
    }

    private void PlaceSameBlock()
    {

        BlockType targetBlock = GetTargetBlock(10.0f);
        if (BlockType.IsAirBlock(targetBlock))
        {
            return;
        }
        BlockType targetType = targetBlock;

        if (targetType.isPlant)
        {
            return; // Don't place foliage blocks like grass and flowers.
        }

        PlaceBlock(targetType);
    }

    private void BreakBlock()
    {

        RaycastHit hit;
        Vector3Int blockPos;
        if (RaycastToBlock(10.0f, out hit, false, out blockPos))
        {

            BreakBlock(hit, blockPos);
        }
    }

    public static Vector3 bp;
    public AudioClip clip1, clip2;
    public void BreakBlock(RaycastHit hit, Vector3Int blockPos)
    {



     
        if (hit.collider.CompareTag("TNT"))
        {
            hit.collider.GetComponent<TNT>().FireTNT();
            return;
        }

        if (hit.collider.CompareTag("luaCube"))
        {

            ParticleSystem effect = ObjPool.GetComponent<ParticleSystem>(breakEffect, hit.collider.transform.position, Quaternion.identity, 0.5f);
            PlayBlockSound(BlockType.Plank, hit.collider.transform.position);
            ParticleSystem.MainModule main = effect.main;

            main.startColor = BlockType.Plank.breakParticleColors;

            //  effect.Play();
            monitorController.OnBlockDestroyed();
            GameObject.Destroy(hit.collider.gameObject);

        }

        WorldChunk chunk = null;
        BlockType breakingBlock = null;




        if (hit.collider.CompareTag("Water") || hit.collider.CompareTag("OpaqueMesh") || hit.collider.CompareTag("Foliage"))
        {
         
            chunk = null;
            breakingBlock = ChunkManager.Instance.GetBlockAtPosition(blockPos, ref chunk);

            if (!BlockType.IsAirBlock(breakingBlock))
            {

                ParticleSystem effect = ObjPool.GetComponent<ParticleSystem>(breakEffect, blockPos, Quaternion.identity, 0.5f);
                if (breakingBlock == BlockType.Water)
                {
                    effect.transform.Translate(0, 0.5f, 0);
                }

                else
                {
                    PlayBlockSound(breakingBlock, blockPos);
                }




                ParticleSystem.MainModule main = effect.main;

                main.startColor = breakingBlock.breakParticleColors;
                //  effect.Play();
                monitorController.OnBlockDestroyed();

                Vector3Int localpos = chunk.WorldToLocalPosition(blockPos);

                //  chunk.isExternalBlock[localpos.x, localpos.y, localpos.z] = false;
                if (!chunk.DestroyBlocks.Contains(localpos))
                {
                    chunk.DestroyBlocks.Add(localpos);
                }
                ChunkManager.destroyChunks.Add(chunk);

                if (breakingBlock.blockName == BlockNameEnum.Diamond_Ore)
                {
                    monitorController.OnDiamondMined();
                }

            }


        }



    }

   
    private static void PlayBlockSound(BlockType blockTypeName, Vector3 position)
    {
        AudioClip clip = blockTypeName.digClip;
        if (clip != null)
        {

            AudioSource.PlayClipAtPoint(clip, position);
        }
    }
    public LayerMask layer;

    private bool  RaycastToBlock(in float maxDistance, out RaycastHit hit, in bool getEmptyBlock, out Vector3Int hitBlockPosition)
    {


        // Does the ray intersect any objects excluding the player layer
        Vector3 direction = Camera.main.transform.TransformDirection(Vector3.forward);
        Physics.queriesHitBackfaces = true;
        if (Physics.Raycast(Camera.main.transform.position, direction, out hit, 5, layer))
        {


            if (hit.distance <= maxDistance)
            {
                Vector3 offset = getEmptyBlock ? direction * -0.01f : direction * 0.01f;
                hitBlockPosition = Vector3Int.RoundToInt(hit.point + offset);
                Physics.queriesHitBackfaces = false;
                return true;
            }
        }
        Physics.queriesHitBackfaces = false;
        hitBlockPosition = Vector3Int.zero;
        return false;
    }




    /////////////////////// Hotbar ///////////////////////
    KeyCode[] keycodes;
    private void HandleHotbarSelection()
    {

        for (int i = 0; i < keycodes.Length; i++)
        {
            if (Input.GetKeyDown(keycodes[i]))
            {
                hotbarController.SelectItem(i);
                break;
            }
        }
    }

    private void HandleRightMouseClick()
    {
        switch (hotbarController.SeletedItem.Type)
        {
           
            case SlotItem.SlotItemType.TNT:
                PlaceTNT();
                // LaunchTNT();
                break;
           
           
            case SlotItem.SlotItemType.Stone:
                PlaceBlock(BlockType.NameToBlockType[BlockNameEnum.Stone]);
                break;
            case SlotItem.SlotItemType.Sand:
                PlaceBlock(BlockType.NameToBlockType[BlockNameEnum.Sand]);
                break;
            case SlotItem.SlotItemType.Gravel:
                PlaceBlock(BlockType.NameToBlockType[BlockNameEnum.Gravel]);
                break;
            case SlotItem.SlotItemType.Bedrock:
                PlaceBlock(BlockType.NameToBlockType[BlockNameEnum.Bedrock]);
                break;
            case SlotItem.SlotItemType.Dirt:
                PlaceBlock(BlockType.NameToBlockType[BlockNameEnum.Dirt]);
                break;
            case SlotItem.SlotItemType.Cobblestone:
                PlaceBlock(BlockType.NameToBlockType[BlockNameEnum.Cobblestone]);
                break;
            case SlotItem.SlotItemType.Plank:
                PlaceBlock(BlockType.NameToBlockType[BlockNameEnum.Plank]);
                break;
            case SlotItem.SlotItemType.CopyBlock:
                PlaceSameBlock();
                break;
            default:
                PlaceSameBlock();
                break;
        }
    }

 

   



}
