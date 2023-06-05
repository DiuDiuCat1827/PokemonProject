using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterAnimator : MonoBehaviour
{

    [SerializeField] List<Sprite> walkDwonSprites;
    [SerializeField] List<Sprite> walkUpSprites;
    [SerializeField] List<Sprite> walkRightSprites;
    [SerializeField] List<Sprite> walkLeftSprites;
    [SerializeField] FacingDirection defaultDirection = FacingDirection.Down;

    //Paramtes
    public float MoveX { get; set; }
    public float MoveY { get; set; }
    public bool IsMoving { get; set; }

    //States
    SpriteAnimator walkDownAnim;
    SpriteAnimator walkUpAnim;
    SpriteAnimator walkRightAnim;
    SpriteAnimator walkLeftAnim;

    SpriteAnimator currentAnim;

    bool wasPreviouslyMoving;

    //Refrences
    SpriteRenderer spriteRenderer;

    public void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        walkDownAnim = new SpriteAnimator(walkDwonSprites, spriteRenderer);
        walkUpAnim = new SpriteAnimator(walkUpSprites, spriteRenderer);
        walkRightAnim = new SpriteAnimator(walkRightSprites, spriteRenderer);
        walkLeftAnim = new SpriteAnimator(walkLeftSprites, spriteRenderer);
        SetFacingDirection(defaultDirection);

        currentAnim = walkRightAnim;
    }

    private void Update()
    {
        var preAnim = currentAnim;

        if(MoveX == 1)
        {
            currentAnim = walkRightAnim;
        }else if (MoveX == -1)
        {
            currentAnim = walkLeftAnim;
        }else if(MoveY == 1)
        {
            currentAnim = walkUpAnim;
        }else if (MoveY == -1)
        {
            currentAnim = walkDownAnim;
        }

        if(currentAnim != preAnim || IsMoving != wasPreviouslyMoving)
        {
            currentAnim.Start();
        }

        if (IsMoving)
        {
            currentAnim.HandleUpdate();
        }
        else
        {
            spriteRenderer.sprite = currentAnim.Frames[0];
        }

        wasPreviouslyMoving = IsMoving;
    }

    public void SetFacingDirection(FacingDirection dir)
    {
        if (dir == FacingDirection.Right)
        {
            MoveX = 1;
        }else if(dir == FacingDirection.Left)
        {
            MoveX = -1;
        }else if (dir == FacingDirection.Down)
        {
            MoveY = -1;
        }else if (dir == FacingDirection.Up)
        {
            MoveY = 1;
        }
    }

    public FacingDirection GefaultDirection
    {
        get => defaultDirection;
    }
}

public enum FacingDirection
{
    Up,Down,Left,Right
}