import { Action, ActionReducer } from '@ngrx/store';
import { sessionActions } from './session/session.actions';

// When the session ends nothing of the previous account may stay in the store:
// every slice goes back to its initial state (the page is not reloaded any more)
export function resetOnSessionEnded<S>(
    reducer: ActionReducer<S, Action>,
): ActionReducer<S, Action> {
    return (state, action) =>
        reducer(
            action.type === sessionActions.ended.type ? undefined : state,
            action,
        );
}
