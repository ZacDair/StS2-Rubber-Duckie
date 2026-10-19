extends SceneTree


func _initialize() -> void:
	call_deferred("verify_pack")


func fail(message: String) -> void:
	push_error(message)
	quit(1)


func verify_pack() -> void:
	var args := OS.get_cmdline_user_args()
	if args.size() != 1 or not ProjectSettings.load_resource_pack(args[0]):
		fail("Could not mount the release PCK.")
		return

	var stream := load("res://RubberDuckie/audio/SFX_squeak.wav") as AudioStreamWAV
	if stream == null or stream.get_length() <= 0:
		fail("The packaged squeak is missing or empty.")
		return

	var icon := load("res://RubberDuckie/mod_image.png") as Texture2D
	if icon == null or icon.get_width() <= 0:
		fail("The packaged mod icon is missing or empty.")
		return

	var player := AudioStreamPlayer.new()
	root.add_child(player)
	player.stream = stream
	player.play()
	if not player.playing:
		fail("The packaged squeak could not start playback.")
		return

	await create_timer(stream.get_length() + 0.2).timeout
	if player.playing:
		fail("The packaged squeak did not finish playback.")
		return

	print("PASS release PCK: squeak loaded and played; mod icon loaded.")
	quit(0)
