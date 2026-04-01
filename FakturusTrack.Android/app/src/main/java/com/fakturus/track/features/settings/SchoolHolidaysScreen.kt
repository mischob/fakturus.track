package com.fakturus.track.features.settings

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.filled.Delete
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.DatePicker
import androidx.compose.material3.DatePickerDialog
import androidx.compose.material3.ElevatedCard
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.FloatingActionButton
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.material3.TopAppBar
import androidx.compose.material3.rememberDatePickerState
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import androidx.compose.material.icons.filled.CloudDownload
import androidx.compose.material3.Button
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.runtime.rememberCoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import org.json.JSONArray
import java.net.URL
import java.time.Instant
import java.time.LocalDate
import java.time.ZoneId
import java.time.format.DateTimeFormatter
import java.util.Locale

data class SchoolHolidayPeriod(
    val id: String = java.util.UUID.randomUUID().toString(),
    val name: String,
    val startDate: LocalDate,
    val endDate: LocalDate
)

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun SchoolHolidaysScreen(
    onNavigateBack: () -> Unit,
    bundesland: String = "NW"
) {
    // Local state - backend CRUD will be added later
    var holidays by remember { mutableStateOf(listOf<SchoolHolidayPeriod>()) }
    var showAddDialog by remember { mutableStateOf(false) }
    var editingHoliday by remember { mutableStateOf<SchoolHolidayPeriod?>(null) }
    var isImporting by remember { mutableStateOf(false) }
    var importError by remember { mutableStateOf<String?>(null) }
    val scope = rememberCoroutineScope()

    val dateFormatter = remember {
        DateTimeFormatter.ofPattern("dd.MM.yyyy", Locale.GERMAN)
    }

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text("Schulferien") },
                navigationIcon = {
                    IconButton(onClick = onNavigateBack) {
                        Icon(Icons.AutoMirrored.Filled.ArrowBack, "Zurueck")
                    }
                }
            )
        },
        floatingActionButton = {
            FloatingActionButton(onClick = { showAddDialog = true }) {
                Icon(Icons.Default.Add, "Hinzufuegen")
            }
        }
    ) { innerPadding ->
        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(innerPadding)
        ) {
            // Import button
            Button(
                onClick = {
                    isImporting = true
                    importError = null
                    scope.launch {
                        try {
                            val year = LocalDate.now().year
                            val stateCode = bundesland.uppercase()
                            val json = withContext(Dispatchers.IO) {
                                URL("https://ferien-api.de/api/v1/holidays/$stateCode/$year").readText()
                            }
                            val arr = JSONArray(json)
                            val imported = mutableListOf<SchoolHolidayPeriod>()
                            for (i in 0 until arr.length()) {
                                val obj = arr.getJSONObject(i)
                                val name = obj.getString("name").split(" ").first()
                                    .replaceFirstChar { it.uppercase() }
                                imported.add(
                                    SchoolHolidayPeriod(
                                        name = name,
                                        startDate = LocalDate.parse(obj.getString("start")),
                                        endDate = LocalDate.parse(obj.getString("end"))
                                    )
                                )
                            }
                            holidays = imported
                        } catch (e: Exception) {
                            importError = "Import fehlgeschlagen: ${e.message}"
                        }
                        isImporting = false
                    }
                },
                enabled = !isImporting,
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(horizontal = 16.dp, vertical = 8.dp)
            ) {
                if (isImporting) {
                    CircularProgressIndicator(
                        modifier = Modifier.padding(end = 8.dp),
                        strokeWidth = 2.dp,
                        color = MaterialTheme.colorScheme.onPrimary
                    )
                } else {
                    Icon(Icons.Default.CloudDownload, contentDescription = null, modifier = Modifier.padding(end = 8.dp))
                }
                Text("Schulferien automatisch laden")
            }

            importError?.let {
                Text(
                    text = it,
                    color = MaterialTheme.colorScheme.error,
                    style = MaterialTheme.typography.bodySmall,
                    modifier = Modifier.padding(horizontal = 16.dp)
                )
            }

            Text(
                text = "Laedt Schulferien fuer $bundesland von ferien-api.de",
                style = MaterialTheme.typography.bodySmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant,
                modifier = Modifier.padding(horizontal = 16.dp, vertical = 4.dp)
            )

        if (holidays.isEmpty()) {
            Column(
                modifier = Modifier
                    .fillMaxSize()
                    .padding(innerPadding),
                verticalArrangement = Arrangement.Center,
                horizontalAlignment = Alignment.CenterHorizontally
            ) {
                Text(
                    text = "Keine Schulferien eingetragen",
                    style = MaterialTheme.typography.bodyLarge,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )
                Spacer(Modifier.height(8.dp))
                Text(
                    text = "Tippen Sie auf +, um Schulferien hinzuzufuegen",
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )
            }
        } else {
            LazyColumn(
                modifier = Modifier
                    .fillMaxSize()
                    .padding(innerPadding)
                    .padding(horizontal = 16.dp),
                verticalArrangement = Arrangement.spacedBy(8.dp)
            ) {
                item { Spacer(Modifier.height(8.dp)) }
                items(holidays, key = { it.id }) { holiday ->
                    ElevatedCard(
                        modifier = Modifier.fillMaxWidth()
                    ) {
                        Row(
                            modifier = Modifier
                                .fillMaxWidth()
                                .padding(16.dp),
                            horizontalArrangement = Arrangement.SpaceBetween,
                            verticalAlignment = Alignment.CenterVertically
                        ) {
                            Column(modifier = Modifier.weight(1f)) {
                                Text(
                                    text = holiday.name,
                                    style = MaterialTheme.typography.titleSmall
                                )
                                Text(
                                    text = "${holiday.startDate.format(dateFormatter)} - ${holiday.endDate.format(dateFormatter)}",
                                    style = MaterialTheme.typography.bodySmall,
                                    color = MaterialTheme.colorScheme.onSurfaceVariant
                                )
                            }
                            IconButton(onClick = {
                                holidays = holidays.filter { it.id != holiday.id }
                            }) {
                                Icon(
                                    Icons.Default.Delete,
                                    contentDescription = "Loeschen",
                                    tint = MaterialTheme.colorScheme.error
                                )
                            }
                        }
                    }
                }
                item { Spacer(Modifier.height(80.dp)) }
            }
        }
        } // end Column
    }

    // Add/Edit Dialog
    if (showAddDialog || editingHoliday != null) {
        SchoolHolidayDialog(
            initial = editingHoliday,
            onDismiss = {
                showAddDialog = false
                editingHoliday = null
            },
            onSave = { period ->
                if (editingHoliday != null) {
                    holidays = holidays.map {
                        if (it.id == period.id) period else it
                    }
                } else {
                    holidays = holidays + period
                }
                showAddDialog = false
                editingHoliday = null
            }
        )
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun SchoolHolidayDialog(
    initial: SchoolHolidayPeriod?,
    onDismiss: () -> Unit,
    onSave: (SchoolHolidayPeriod) -> Unit
) {
    var name by remember { mutableStateOf(initial?.name ?: "") }
    var startDate by remember { mutableStateOf(initial?.startDate ?: LocalDate.now()) }
    var endDate by remember { mutableStateOf(initial?.endDate ?: LocalDate.now().plusDays(7)) }
    var showStartPicker by remember { mutableStateOf(false) }
    var showEndPicker by remember { mutableStateOf(false) }

    val dateFormatter = remember {
        DateTimeFormatter.ofPattern("dd.MM.yyyy", Locale.GERMAN)
    }

    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text(if (initial != null) "Schulferien bearbeiten" else "Schulferien hinzufuegen") },
        text = {
            Column(verticalArrangement = Arrangement.spacedBy(12.dp)) {
                OutlinedTextField(
                    value = name,
                    onValueChange = { name = it },
                    label = { Text("Bezeichnung") },
                    modifier = Modifier.fillMaxWidth(),
                    singleLine = true
                )
                TextButton(onClick = { showStartPicker = true }) {
                    Text("Von: ${startDate.format(dateFormatter)}")
                }
                TextButton(onClick = { showEndPicker = true }) {
                    Text("Bis: ${endDate.format(dateFormatter)}")
                }
            }
        },
        confirmButton = {
            TextButton(
                onClick = {
                    if (name.isNotBlank()) {
                        onSave(
                            SchoolHolidayPeriod(
                                id = initial?.id ?: java.util.UUID.randomUUID().toString(),
                                name = name,
                                startDate = startDate,
                                endDate = endDate
                            )
                        )
                    }
                },
                enabled = name.isNotBlank()
            ) {
                Text("Speichern")
            }
        },
        dismissButton = {
            TextButton(onClick = onDismiss) {
                Text("Abbrechen")
            }
        }
    )

    if (showStartPicker) {
        val state = rememberDatePickerState(
            initialSelectedDateMillis = startDate.atStartOfDay(ZoneId.systemDefault()).toInstant().toEpochMilli()
        )
        DatePickerDialog(
            onDismissRequest = { showStartPicker = false },
            confirmButton = {
                TextButton(onClick = {
                    state.selectedDateMillis?.let {
                        startDate = Instant.ofEpochMilli(it).atZone(ZoneId.systemDefault()).toLocalDate()
                    }
                    showStartPicker = false
                }) { Text("OK") }
            },
            dismissButton = {
                TextButton(onClick = { showStartPicker = false }) { Text("Abbrechen") }
            }
        ) {
            DatePicker(state = state)
        }
    }

    if (showEndPicker) {
        val state = rememberDatePickerState(
            initialSelectedDateMillis = endDate.atStartOfDay(ZoneId.systemDefault()).toInstant().toEpochMilli()
        )
        DatePickerDialog(
            onDismissRequest = { showEndPicker = false },
            confirmButton = {
                TextButton(onClick = {
                    state.selectedDateMillis?.let {
                        endDate = Instant.ofEpochMilli(it).atZone(ZoneId.systemDefault()).toLocalDate()
                    }
                    showEndPicker = false
                }) { Text("OK") }
            },
            dismissButton = {
                TextButton(onClick = { showEndPicker = false }) { Text("Abbrechen") }
            }
        ) {
            DatePicker(state = state)
        }
    }
}
