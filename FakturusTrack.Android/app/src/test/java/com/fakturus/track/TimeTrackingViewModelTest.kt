package com.fakturus.track

import com.fakturus.track.models.WorkSessionEntity
import com.fakturus.track.models.WorkSessionDTO
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test
import java.time.Instant
import java.time.LocalDate

class TimeTrackingViewModelTest {

    // WorkSessionEntity computed properties

    @Test
    fun `isRunning returns true when not finished and no stopTime`() {
        val session = WorkSessionEntity(
            date = "2026-03-29",
            startTime = Instant.now().toString(),
            stopTime = null,
            isFinished = false
        )
        assertTrue(session.isRunning)
    }

    @Test
    fun `isRunning returns false when finished`() {
        val session = WorkSessionEntity(
            date = "2026-03-29",
            startTime = Instant.now().toString(),
            isFinished = true
        )
        assertFalse(session.isRunning)
    }

    @Test
    fun `isRunning returns false when stopTime is set`() {
        val session = WorkSessionEntity(
            date = "2026-03-29",
            startTime = Instant.now().toString(),
            stopTime = Instant.now().toString(),
            isFinished = false
        )
        assertFalse(session.isRunning)
    }

    @Test
    fun `durationMinutes calculates correctly with stopTime`() {
        val start = Instant.parse("2026-03-29T08:00:00Z")
        val stop = Instant.parse("2026-03-29T17:00:00Z") // 9 hours later
        val session = WorkSessionEntity(
            date = "2026-03-29",
            startTime = start.toString(),
            stopTime = stop.toString(),
            isFinished = true
        )
        assertEquals(540L, session.durationMinutes) // 9h = 540min
    }

    @Test
    fun `netDurationMinutes subtracts pauseMinutes`() {
        val start = Instant.parse("2026-03-29T08:00:00Z")
        val stop = Instant.parse("2026-03-29T17:00:00Z") // 9h = 540min
        val session = WorkSessionEntity(
            date = "2026-03-29",
            startTime = start.toString(),
            stopTime = stop.toString(),
            pauseMinutes = 30,
            isFinished = true
        )
        assertEquals(510L, session.netDurationMinutes) // 540 - 30 = 510min = 8.5h
    }

    @Test
    fun `netDurationMinutes does not go below zero`() {
        val start = Instant.parse("2026-03-29T08:00:00Z")
        val stop = Instant.parse("2026-03-29T08:10:00Z") // 10 minutes
        val session = WorkSessionEntity(
            date = "2026-03-29",
            startTime = start.toString(),
            stopTime = stop.toString(),
            pauseMinutes = 60, // more pause than duration
            isFinished = true
        )
        assertEquals(0L, session.netDurationMinutes)
    }

    @Test
    fun `monthKey formats correctly in German`() {
        val session = WorkSessionEntity(
            date = "2026-03-29",
            startTime = Instant.now().toString(),
            isFinished = true
        )
        assertEquals("März 2026", session.monthKey)
    }

    // DTO conversion

    @Test
    fun `toDTO converts fields correctly`() {
        val id = "test-id-123"
        val session = WorkSessionEntity(
            id = id,
            date = "2026-03-29",
            startTime = "2026-03-29T08:00:00Z",
            stopTime = "2026-03-29T17:00:00Z",
            pauseMinutes = 30,
            isFinished = true
        )
        val dto = session.toDTO()

        assertEquals(id, dto.id)
        assertEquals("2026-03-29", dto.date)
        assertEquals("2026-03-29T08:00:00Z", dto.startTime)
        assertEquals("2026-03-29T17:00:00Z", dto.stopTime)
        assertEquals(30, dto.pauseMinutes)
    }

    @Test
    fun `toDTO handles null stopTime`() {
        val session = WorkSessionEntity(
            date = "2026-03-29",
            startTime = "2026-03-29T08:00:00Z",
            stopTime = null,
            isFinished = false
        )
        val dto = session.toDTO()
        assertNull(dto.stopTime)
    }

    @Test
    fun `WorkSessionDTO toEntity converts correctly`() {
        val dto = WorkSessionDTO(
            id = "abc-123",
            userId = "user1",
            date = "2026-03-29",
            startTime = "2026-03-29T08:00:00Z",
            stopTime = "2026-03-29T17:00:00Z",
            pauseMinutes = 15
        )
        val entity = dto.toEntity()

        assertEquals("abc-123", entity.id)
        assertEquals("user1", entity.userId)
        assertEquals("2026-03-29", entity.date)
        assertEquals("2026-03-29T08:00:00Z", entity.startTime)
        assertEquals("2026-03-29T17:00:00Z", entity.stopTime)
        assertEquals(15, entity.pauseMinutes)
        assertFalse(entity.isPendingSync)
        assertTrue(entity.isSynced)
        assertTrue(entity.isFinished) // stopTime != null
    }

    @Test
    fun `WorkSessionDTO toEntity sets isFinished false when no stopTime`() {
        val dto = WorkSessionDTO(
            id = "abc-123",
            date = "2026-03-29",
            startTime = "2026-03-29T08:00:00Z",
            stopTime = null
        )
        val entity = dto.toEntity()
        assertFalse(entity.isFinished)
        assertNull(entity.stopTime)
    }
}
