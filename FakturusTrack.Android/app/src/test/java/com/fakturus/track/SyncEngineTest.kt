package com.fakturus.track

import com.fakturus.track.models.SyncVacationDaysResponse
import com.fakturus.track.models.VacationDayDTO
import com.fakturus.track.models.VacationDaySyncItem
import com.fakturus.track.models.WorkSessionDTO
import com.fakturus.track.models.WorkSessionSyncItem
import kotlinx.serialization.encodeToString
import kotlinx.serialization.json.Json
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNull
import org.junit.Test

class SyncEngineTest {

    private val json = Json { ignoreUnknownKeys = true }

    @Test
    fun `workSessionDTO serialization with PascalCase`() {
        val dto = WorkSessionSyncItem(
            id = "abc-123",
            date = "2026-03-29",
            startTime = "2026-03-29T08:00:00Z",
            stopTime = "2026-03-29T17:00:00Z",
            pauseMinutes = 30
        )

        val jsonString = json.encodeToString(dto)

        // Verify PascalCase keys
        assert(jsonString.contains("\"Id\""))
        assert(jsonString.contains("\"Date\""))
        assert(jsonString.contains("\"StartTime\""))
        assert(jsonString.contains("\"StopTime\""))
        assert(jsonString.contains("\"PauseMinutes\""))

        // Round-trip
        val decoded = json.decodeFromString<WorkSessionSyncItem>(jsonString)
        assertEquals(dto.id, decoded.id)
        assertEquals(dto.date, decoded.date)
        assertEquals(dto.startTime, decoded.startTime)
        assertEquals(dto.stopTime, decoded.stopTime)
        assertEquals(dto.pauseMinutes, decoded.pauseMinutes)
    }

    @Test
    fun `workSessionDTO deserialization from server JSON`() {
        val serverJson = """
            {
                "Id": "abc-123",
                "UserId": "user1",
                "Date": "2026-03-29",
                "StartTime": "2026-03-29T08:00:00Z",
                "StopTime": "2026-03-29T17:00:00Z",
                "PauseMinutes": 30,
                "CreatedAt": "2026-03-29T08:00:00Z",
                "UpdatedAt": "2026-03-29T17:00:00Z"
            }
        """.trimIndent()

        val dto = json.decodeFromString<WorkSessionDTO>(serverJson)

        assertEquals("abc-123", dto.id)
        assertEquals("user1", dto.userId)
        assertEquals("2026-03-29", dto.date)
        assertEquals("2026-03-29T08:00:00Z", dto.startTime)
        assertEquals("2026-03-29T17:00:00Z", dto.stopTime)
        assertEquals(30, dto.pauseMinutes)
    }

    @Test
    fun `vacationDayDTO serialization with PascalCase`() {
        val dto = VacationDaySyncItem(
            id = "vac-123",
            date = "2026-04-01",
            createdAt = "2026-03-29T10:00:00Z",
            updatedAt = "2026-03-29T10:00:00Z",
            syncedAt = null
        )

        val jsonString = json.encodeToString(dto)

        assert(jsonString.contains("\"Id\""))
        assert(jsonString.contains("\"Date\""))
        assert(jsonString.contains("\"CreatedAt\""))
        assert(jsonString.contains("\"UpdatedAt\""))

        val decoded = json.decodeFromString<VacationDaySyncItem>(jsonString)
        assertEquals(dto.id, decoded.id)
        assertEquals(dto.date, decoded.date)
        assertNull(decoded.syncedAt)
    }

    @Test
    fun `vacationDayDTO deserialization from server JSON`() {
        val serverJson = """
            {
                "Id": "vac-456",
                "UserId": "user1",
                "Date": "2026-04-01",
                "CreatedAt": "2026-03-29T10:00:00Z",
                "UpdatedAt": "2026-03-29T10:00:00Z",
                "SyncedAt": "2026-03-29T10:05:00Z"
            }
        """.trimIndent()

        val dto = json.decodeFromString<VacationDayDTO>(serverJson)

        assertEquals("vac-456", dto.id)
        assertEquals("user1", dto.userId)
        assertEquals("2026-04-01", dto.date)
        assertEquals("2026-03-29T10:05:00Z", dto.syncedAt)
    }

    @Test
    fun `syncVacationDaysResponse with deletedIds`() {
        val serverJson = """
            {
                "ServerVacationDays": [
                    {
                        "Id": "vac-1",
                        "Date": "2026-04-01"
                    },
                    {
                        "Id": "vac-2",
                        "Date": "2026-04-02"
                    }
                ],
                "DeletedIds": ["vac-old-1", "vac-old-2"]
            }
        """.trimIndent()

        val response = json.decodeFromString<SyncVacationDaysResponse>(serverJson)

        assertEquals(2, response.serverVacationDays.size)
        assertEquals("vac-1", response.serverVacationDays[0].id)
        assertEquals("vac-2", response.serverVacationDays[1].id)
        assertEquals(2, response.deletedIds.size)
        assertEquals("vac-old-1", response.deletedIds[0])
        assertEquals("vac-old-2", response.deletedIds[1])
    }

    @Test
    fun `syncVacationDaysResponse with empty deletedIds`() {
        val serverJson = """
            {
                "ServerVacationDays": [],
                "DeletedIds": []
            }
        """.trimIndent()

        val response = json.decodeFromString<SyncVacationDaysResponse>(serverJson)

        assertEquals(0, response.serverVacationDays.size)
        assertEquals(0, response.deletedIds.size)
    }
}
