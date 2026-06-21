extends Node2D

@export var fuse_time := 2.0

func _ready():
	await get_tree().create_timer(fuse_time).timeout
	explode()

func explode():
	print("BOOM!")

	var directions = [
		Vector2.ZERO,
		Vector2.UP * 32,
		Vector2.DOWN * 32,
		Vector2.LEFT * 32,
		Vector2.RIGHT * 32
	]

	for dir in directions:
		var fire = ColorRect.new()
		fire.color = Color.ORANGE
		fire.size = Vector2(32, 32)
		fire.position = position + dir

		get_parent().add_child(fire)

		await get_tree().create_timer(0.3).timeout
		fire.queue_free()

	queue_free()
