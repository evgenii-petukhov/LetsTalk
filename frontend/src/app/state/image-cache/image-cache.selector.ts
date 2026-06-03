import { createFeatureSelector } from '@ngrx/store';
import { ImageCacheEntry } from '../../models/image-cache-entry';

export const selectImageCache = createFeatureSelector<readonly ImageCacheEntry[]>('imageCache');
