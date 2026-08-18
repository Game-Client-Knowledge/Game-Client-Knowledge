local StateMachine = {}
StateMachine.__index = StateMachine

function StateMachine.new(states, initialState, context)
    assert(type(states) == "table", "states must be a table")
    assert(states[initialState], "initial state does not exist")

    local self = setmetatable({
        states = states,
        current = initialState,
        context = context,
        transitioning = false,
    }, StateMachine)

    local state = states[initialState]
    if state.enter then
        state.enter(context)
    end

    return self
end

function StateMachine:transition(nextState)
    assert(not self.transitioning, "state transition is reentrant")
    assert(self.states[nextState], "target state does not exist")

    if nextState == self.current then
        return
    end

    self.transitioning = true

    local previous = self.states[self.current]
    if previous.exit then
        previous.exit(self.context)
    end

    self.current = nextState

    local current = self.states[self.current]
    if current.enter then
        current.enter(self.context)
    end

    self.transitioning = false
end

function StateMachine:update(dt)
    local state = assert(self.states[self.current])
    if not state.update then
        return
    end

    local nextState = state.update(self.context, dt)
    if nextState then
        self:transition(nextState)
    end
end

return StateMachine
