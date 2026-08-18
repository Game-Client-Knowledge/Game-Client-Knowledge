local Scheduler = {}
Scheduler.__index = Scheduler

function Scheduler.new()
    return setmetatable({
        now = 0,
        nextTaskId = 1,
        tasks = {},
    }, Scheduler)
end

function Scheduler:start(fn, owner)
    assert(type(fn) == "function", "task must be a function")

    local task = {
        id = self.nextTaskId,
        coroutine = coroutine.create(fn),
        owner = owner,
        wakeAt = self.now,
        cancelled = false,
    }
    self.nextTaskId = self.nextTaskId + 1
    self.tasks[#self.tasks + 1] = task

    return task
end

function Scheduler:cancel(task)
    if task then
        task.cancelled = true
    end
end

function Scheduler:cancelOwner(owner)
    for _, task in ipairs(self.tasks) do
        if task.owner == owner then
            task.cancelled = true
        end
    end
end

function Scheduler:update(dt)
    self.now = self.now + dt

    for index = #self.tasks, 1, -1 do
        local task = self.tasks[index]

        if not task.cancelled and self.now >= task.wakeAt then
            local ok, waitSeconds = coroutine.resume(task.coroutine)
            if not ok then
                error("task " .. task.id .. " failed: " .. tostring(waitSeconds))
            end

            if coroutine.status(task.coroutine) ~= "dead" then
                assert(
                    type(waitSeconds) == "number" and waitSeconds >= 0,
                    "task must yield a non-negative wait time"
                )
                task.wakeAt = self.now + waitSeconds
            end
        end

        if task.cancelled or coroutine.status(task.coroutine) == "dead" then
            table.remove(self.tasks, index)
        end
    end
end

function Scheduler:count()
    return #self.tasks
end

return Scheduler
