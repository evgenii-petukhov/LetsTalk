import { describe, expect, it, vi } from 'vitest';
import { resetOnSessionEnded } from './reset-on-session-ended.meta-reducer';
import { sessionActions } from './session/session.actions';

describe('resetOnSessionEnded', () => {
    it('should pass the current state to the reducer for other actions', () => {
        const reducer = vi.fn().mockReturnValue('next');
        const state = { loggedInUser: { id: '1' } };
        const action = { type: 'some action' };

        const result = resetOnSessionEnded(reducer)(state, action);

        expect(reducer).toHaveBeenCalledWith(state, action);
        expect(result).toBe('next');
    });

    it('should reset the state when the session ended', () => {
        const reducer = vi.fn().mockReturnValue('initial');
        const state = { loggedInUser: { id: '1' } };
        const action = sessionActions.ended();

        const result = resetOnSessionEnded(reducer)(state, action);

        expect(reducer).toHaveBeenCalledWith(undefined, action);
        expect(result).toBe('initial');
    });
});
