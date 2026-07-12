<script setup lang="ts">
import { RouterLink, RouterView } from 'vue-router'
</script>

<template>
  <a class="skip-link" href="#main-content">Skip to main content</a>
  <div class="app-shell">
    <header class="topbar">
      <RouterLink class="brand" :to="{ name: 'contacts-list' }" aria-label="Phonebook home">
        <span class="brand-mark" aria-hidden="true">P</span>
        <span>Phonebook</span>
      </RouterLink>
      <nav class="topnav" aria-label="Primary navigation">
        <RouterLink :to="{ name: 'contacts-list' }">Contacts</RouterLink>
        <RouterLink :to="{ name: 'contacts-create' }">Create Contact</RouterLink>
      </nav>
    </header>

    <main id="main-content" class="page-shell" tabindex="-1">
      <RouterView />
    </main>
  </div>
</template>
