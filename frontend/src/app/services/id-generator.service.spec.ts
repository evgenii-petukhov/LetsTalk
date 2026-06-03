import { TestBed } from '@angular/core/testing';
import { IdGeneratorService } from './id-generator.service';
import { beforeEach, describe, expect, it } from 'vitest';

describe('IdGeneratorService', () => {
    let service: IdGeneratorService;

    beforeEach(() => {
        TestBed.configureTestingModule({
            providers: [IdGeneratorService],
        });
        service = TestBed.inject(IdGeneratorService);
    });

    it('should be created', () => {
        expect(service).toBeTruthy();
    });

    describe('getNextFakeId', () => {
        it('should return a negative number that decrements with each call', () => {
            // Arrange

            // Act
            const firstId = service.getNextFakeId();
            const secondId = service.getNextFakeId();
            const thirdId = service.getNextFakeId();

            // Assert
            expect(firstId).toBe(-1);
            expect(secondId).toBe(-2);
            expect(thirdId).toBe(-3);
        });
    });

    describe('isFake', () => {
        it('should return true for negative numbers in string form', () => {
            // Arrange

            // Act

            // Assert
            expect(service.isFake('-1')).toBe(true);
            expect(service.isFake('-123')).toBe(true);
        });

        it('should return false for positive numbers in string form', () => {
            // Arrange

            // Act

            // Assert
            expect(service.isFake('1')).toBe(false);
            expect(service.isFake('123')).toBe(false);
        });

        it('should return false for non-numeric strings', () => {
            // Arrange

            // Act

            // Assert
            expect(service.isFake('abc')).toBe(false);
            expect(service.isFake('')).toBe(false);
        });

        it('should return false for zero', () => {
            // Arrange

            // Act

            // Assert
            expect(service.isFake('0')).toBe(false);
        });
    });
});
