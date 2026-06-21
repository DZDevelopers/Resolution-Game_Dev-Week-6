extends CharacterBody2D

@export var speed := 150.0
@export var player_id := 2
@export var bomb_scene : PackedScene

func _physics_process(delta):
	var direction = Vector2.ZERO

	if Input.is_action_pressed("p%d_up" % player_id):
		direction.y -= 1

	if Input.is_action_pressed("p%d_down" % player_id):
		direction.y += 1

	if Input.is_action_pressed("p%d_left" % player_id):
		direction.x -= 1

	if Input.is_action_pressed("p%d_right" % player_id):
		direction.x += 1

	velocity = direction.normalized() * speed
	move_and_slide()

func _process(delta):
	if Input.is_action_just_pressed("p%d_bomb" % player_id):
		place_bomb()

func place_bomb():
	var bomb = bomb_scene.instantiate()

	# Snap to 32x32 grid
	bomb.position = position.snapped(Vector2(32, 32))

	get_parent().add_child(bomb)
