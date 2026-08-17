local EventBus = require("event_bus")
local Scheduler = require("scheduler")
local StateMachine = require("state_machine")

print("== Lua game client basics ==")

local eventBus = EventBus.new()
local scheduler = Scheduler.new()
local scope = {}

eventBus:on("hp_changed", function(previous, current)
    print(string.format("hp changed: %d -> %d", previous, current))
end, scope)

local context = {
    shouldMove = false,
    distance = 0,
    speed = 4,
}

local states = {
    idle = {
        enter = function()
            print("state enter: idle")
        end,
        update = function(ctx)
            if ctx.shouldMove then
                return "moving"
            end
        end,
        exit = function()
            print("state exit: idle")
        end,
    },
    moving = {
        enter = function()
            print("state enter: moving")
        end,
        update = function(ctx, dt)
            ctx.distance = ctx.distance + ctx.speed * dt
            print(string.format("moving distance: %.1f", ctx.distance))

            if ctx.distance >= 3 then
                return "arrived"
            end
        end,
        exit = function()
            print("state exit: moving")
        end,
    },
    arrived = {
        enter = function()
            print("state enter: arrived")
        end,
    },
}

local movement = StateMachine.new(states, "idle", context)

scheduler:start(function()
    print("quest: start")
    coroutine.yield(0.5)
    print("quest: dialogue")
    coroutine.yield(0.5)
    print("quest: complete")
end, scope)

eventBus:emit("hp_changed", 100, 72)

for frame = 1, 5 do
    print("frame " .. frame)

    if frame == 2 then
        context.shouldMove = true
    end

    movement:update(0.25)
    scheduler:update(0.25)
end

eventBus:offOwner(scope)
scheduler:cancelOwner(scope)

print("active tasks: " .. scheduler:count())
