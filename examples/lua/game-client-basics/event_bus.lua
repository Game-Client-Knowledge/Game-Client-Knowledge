local EventBus = {}
EventBus.__index = EventBus

function EventBus.new()
    return setmetatable({
        nextTokenId = 1,
        listeners = {},
    }, EventBus)
end

function EventBus:on(eventName, callback, owner)
    assert(type(eventName) == "string", "event name must be a string")
    assert(type(callback) == "function", "event callback must be a function")

    local token = {
        id = self.nextTokenId,
        eventName = eventName,
        callback = callback,
        owner = owner,
        active = true,
    }
    self.nextTokenId = self.nextTokenId + 1

    local eventListeners = self.listeners[eventName]
    if not eventListeners then
        eventListeners = {}
        self.listeners[eventName] = eventListeners
    end
    eventListeners[#eventListeners + 1] = token

    return token
end

function EventBus:off(token)
    if not token or not token.active then
        return
    end

    token.active = false
    token.callback = nil
    token.owner = nil
end

function EventBus:offOwner(owner)
    for _, eventListeners in pairs(self.listeners) do
        for _, token in ipairs(eventListeners) do
            if token.active and token.owner == owner then
                self:off(token)
            end
        end
    end
end

function EventBus:emit(eventName, ...)
    local eventListeners = self.listeners[eventName]
    if not eventListeners then
        return
    end

    -- New listeners do not participate in the current emission.
    local snapshot = {}
    for _, token in ipairs(eventListeners) do
        if token.active then
            snapshot[#snapshot + 1] = token
        end
    end

    for _, token in ipairs(snapshot) do
        if token.active then
            token.callback(...)
        end
    end

    self:_compact(eventName)
end

function EventBus:_compact(eventName)
    local eventListeners = self.listeners[eventName]
    local writeIndex = 1

    for readIndex = 1, #eventListeners do
        local token = eventListeners[readIndex]
        if token.active then
            eventListeners[writeIndex] = token
            writeIndex = writeIndex + 1
        end
    end

    for index = writeIndex, #eventListeners do
        eventListeners[index] = nil
    end

    if #eventListeners == 0 then
        self.listeners[eventName] = nil
    end
end

return EventBus
