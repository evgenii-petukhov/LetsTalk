import { createReducer, on } from '@ngrx/store';
import { ImageCacheEntry } from '../../models/image-cache-entry';
import { imageCacheActions } from './image-cache.actions';

export const initialState: readonly ImageCacheEntry[] = [];

export const imageCacheReducer = createReducer(
    initialState,
    on(imageCacheActions.add, (state, { image }) => {
        const existing = state.find((i) => i.imageId === image.imageId);
        return existing ? state : [...state, image];
    }),
);
